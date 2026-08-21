using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class BadgeService(
    IAppDbContext db,
    INotificationService notificationService,
    ILogger<BadgeService> logger) : IBadgeService
{
    public async Task CheckAndAwardBadgesAsync(Guid userId, Guid problemId, CancellationToken ct = default)
    {
        var allBadges = await db.Badges.ToListAsync(ct);
        var earnedSlugs = await db.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.Badge!.Slug)
            .ToListAsync(ct);

        foreach (var badge in allBadges)
        {
            if (earnedSlugs.Contains(badge.Slug))
                continue;

            var earned = badge.Condition switch
            {
                BadgeCondition.FirstAccepted     => await CheckFirstAcceptedAsync(userId, ct),
                BadgeCondition.SpeedSolver       => await CheckSpeedSolverAsync(userId, problemId, ct),
                BadgeCondition.WeekStreak        => await CheckWeekStreakAsync(userId, ct),
                BadgeCondition.Top10Competition  => await CheckTop10CompetitionAsync(userId, ct),
                BadgeCondition.Top3National      => await CheckTop3NationalAsync(userId, ct),
                BadgeCondition.Centurion         => await CheckCenturionAsync(userId, ct),
                BadgeCondition.Mentor            => false, // Triggered via mentor check only
                _                                => false
            };

            if (earned)
                await AwardBadgeAsync(userId, badge, ct);
        }
    }

    public async Task CheckAndAwardMentorBadgeAsync(Guid problemCreatorId, CancellationToken ct = default)
    {
        var badge = await db.Badges.FirstOrDefaultAsync(b => b.Condition == BadgeCondition.Mentor, ct);
        if (badge is null) return;

        var alreadyEarned = await db.UserBadges.AnyAsync(ub => ub.UserId == problemCreatorId && ub.BadgeId == badge.Id, ct);
        if (alreadyEarned) return;

        if (await CheckMentorAsync(problemCreatorId, ct))
            await AwardBadgeAsync(problemCreatorId, badge, ct);
    }

    public async Task RecordInputDownloadAsync(Guid userId, Guid problemId, CancellationToken ct = default)
    {
        var status = await db.UserProblemStatuses
            .FirstOrDefaultAsync(ups => ups.UserId == userId && ups.ProblemId == problemId, ct);

        if (status is null)
        {
            db.UserProblemStatuses.Add(new UserProblemStatus
            {
                UserId = userId,
                ProblemId = problemId,
                InputFirstDownloadedAt = DateTime.UtcNow
            });
        }
        else if (status.InputFirstDownloadedAt is null)
        {
            status.InputFirstDownloadedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(string username, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct)
            ?? throw new NotFoundException($"Utilisateur '{username}' introuvable.");

        return await db.UserBadges
            .Where(ub => ub.UserId == user.Id)
            .Join(db.Badges, ub => ub.BadgeId, b => b.Id, (ub, b) => new BadgeDto(
                b.Id, b.Slug, b.Name, b.Description, b.IconUrl, ub.EarnedAt))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<BadgeDto>> GetAllBadgesAsync(CancellationToken ct = default)
    {
        return await db.Badges
            .Select(b => new BadgeDto(b.Id, b.Slug, b.Name, b.Description, b.IconUrl, null))
            .ToListAsync(ct);
    }

    // --- Private badge condition checks ---

    private async Task<bool> CheckFirstAcceptedAsync(Guid userId, CancellationToken ct)
    {
        return await db.Submissions
            .AnyAsync(s => s.UserId == userId && s.Status == SubmissionStatus.Accepted, ct);
    }

    private async Task<bool> CheckSpeedSolverAsync(Guid userId, Guid problemId, CancellationToken ct)
    {
        var status = await db.UserProblemStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(ups => ups.UserId == userId && ups.ProblemId == problemId, ct);

        if (status?.InputFirstDownloadedAt is null || status.LastAttemptAt is null || !status.Solved)
            return false;

        return (status.LastAttemptAt.Value - status.InputFirstDownloadedAt.Value).TotalMinutes < 30;
    }

    private async Task<bool> CheckWeekStreakAsync(Guid userId, CancellationToken ct)
    {
        var submissionDates = await db.Submissions
            .Where(s => s.UserId == userId)
            .Select(s => s.SubmittedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(30)
            .ToListAsync(ct);

        if (submissionDates.Count < 7)
            return false;

        var streak = 1;
        for (var i = 1; i < submissionDates.Count; i++)
        {
            if ((submissionDates[i - 1] - submissionDates[i]).Days == 1)
            {
                streak++;
                if (streak >= 7) return true;
            }
            else
            {
                streak = 1;
            }
        }

        return false;
    }

    private async Task<bool> CheckTop10CompetitionAsync(Guid userId, CancellationToken ct)
    {
        // Check across all finished competitions whether user is in top 10
        var competitions = await db.Competitions
            .Where(c => c.Status == CompetitionStatus.Finished)
            .Select(c => c.Id)
            .ToListAsync(ct);

        foreach (var compId in competitions)
        {
            var rank = await GetUserRankInCompetitionAsync(userId, compId, ct);
            if (rank is >= 1 and <= 10)
                return true;
        }

        return false;
    }

    private async Task<bool> CheckTop3NationalAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;

        var rank = await db.Users
            .CountAsync(u => u.IsActive && u.Country == user.Country && u.TotalScore > user.TotalScore, ct) + 1;

        return rank <= 3;
    }

    private async Task<bool> CheckCenturionAsync(Guid userId, CancellationToken ct)
    {
        var solvedCount = await db.UserProblemStatuses
            .CountAsync(ups => ups.UserId == userId && ups.Solved, ct);

        return solvedCount >= 100;
    }

    private async Task<bool> CheckMentorAsync(Guid problemCreatorId, CancellationToken ct)
    {
        // Find any problem created by this moderator that was solved by 50+ distinct users
        var problems = await db.Problems
            .Where(p => p.CreatedByUserId == problemCreatorId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var problemId in problems)
        {
            var distinctSolvers = await db.UserProblemStatuses
                .CountAsync(ups => ups.ProblemId == problemId && ups.Solved, ct);

            if (distinctSolvers >= 50)
                return true;
        }

        return false;
    }

    private async Task<int?> GetUserRankInCompetitionAsync(Guid userId, Guid competitionId, CancellationToken ct)
    {
        // Score = sum of points for solved problems in this competition
        var userScore = await db.UserProblemStatuses
            .Where(ups => ups.UserId == userId && ups.Solved)
            .Join(db.Problems, ups => ups.ProblemId, p => p.Id, (ups, p) => p)
            .Where(p => p.CompetitionId == competitionId)
            .SumAsync(p => (int?)p.Points, ct) ?? 0;

        if (userScore == 0) return null;

        // Count how many users scored strictly more
        var problemIds = await db.Problems
            .Where(p => p.CompetitionId == competitionId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var higherScoreCount = await db.UserProblemStatuses
            .Where(ups => ups.Solved && problemIds.Contains(ups.ProblemId) && ups.UserId != userId)
            .GroupBy(ups => ups.UserId)
            .Select(g => g.Join(db.Problems, ups => ups.ProblemId, p => p.Id, (ups, p) => p.Points).Sum())
            .CountAsync(score => score > userScore, ct);

        return higherScoreCount + 1;
    }

    private async Task AwardBadgeAsync(Guid userId, Badge badge, CancellationToken ct)
    {
        db.UserBadges.Add(new UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BadgeId = badge.Id,
            EarnedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Badge '{Slug}' awarded to user {UserId}", badge.Slug, userId);

        await notificationService.CreateAsync(
            userId,
            NotificationType.BadgeEarned,
            $"Badge débloqué : {badge.Name}",
            badge.Description,
            ct);
    }
}
