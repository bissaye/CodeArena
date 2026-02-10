using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class CompetitionService(IAppDbContext db, ILogger<CompetitionService> logger) : ICompetitionService
{
    public async Task<IEnumerable<CompetitionSummaryDto>> GetAllAsync(bool canSeeDraft, CancellationToken ct = default)
    {
        var query = db.Competitions.AsQueryable();

        if (!canSeeDraft)
            query = query.Where(c => c.Status != CompetitionStatus.Draft);

        var competitions = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new CompetitionSummaryDto(
                c.Id,
                c.Name,
                c.StartDate,
                c.StartDate.Add(c.Duration),
                c.Status.ToString(),
                c.Problems.Count
            ))
            .ToListAsync(ct);

        logger.LogDebug("GetAllAsync returned {Count} competitions (canSeeDraft={CanSeeDraft})", competitions.Count, canSeeDraft);
        return competitions;
    }

    public async Task<CompetitionDetailDto?> GetByIdAsync(
        Guid id, bool canSeeDraft, Guid? currentUserId, CancellationToken ct = default)
    {
        var competition = await db.Competitions
            .Where(c => c.Id == id && (canSeeDraft || c.Status != CompetitionStatus.Draft))
            .FirstOrDefaultAsync(ct);

        if (competition is null) return null;

        var problemStats = await db.Problems
            .Where(p => p.CompetitionId == id)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Points,
                TotalSubmissions = p.Submissions.Count,
                AcceptedSubmissions = p.Submissions.Count(s => s.Status == SubmissionStatus.Accepted),
                Solved = currentUserId.HasValue
                    ? (bool?)p.UserStatuses
                        .Where(us => us.UserId == currentUserId.Value)
                        .Select(us => us.Solved)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync(ct);

        var problems = problemStats.Select(p => new ProblemSummaryDto(
            p.Id,
            p.Title,
            p.Points,
            p.TotalSubmissions,
            p.AcceptedSubmissions,
            p.Solved
        ));

        return new CompetitionDetailDto(
            competition.Id,
            competition.Name,
            competition.StartDate,
            competition.StartDate.Add(competition.Duration),
            competition.Status.ToString(),
            problems
        );
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetCompetitionLeaderboardAsync(
        Guid competitionId, int top, CancellationToken ct = default)
    {
        var scores = await db.UserProblemStatuses
            .Where(ups => ups.Problem!.CompetitionId == competitionId && ups.Solved)
            .GroupBy(ups => new { ups.UserId, ups.User!.Username, ups.User.AvatarUrl, ups.User.Country, ups.User.Region })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Username,
                g.Key.AvatarUrl,
                g.Key.Country,
                g.Key.Region,
                Score = g.Sum(ups => ups.Problem!.Points)
            })
            .OrderByDescending(x => x.Score)
            .Take(top)
            .ToListAsync(ct);

        return scores.Select((x, i) => new LeaderboardEntryDto(
            i + 1,
            x.UserId,
            x.Username,
            x.AvatarUrl,
            x.Country,
            x.Region,
            x.Score
        ));
    }

    public async Task<Guid> CreateAsync(CreateCompetitionRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var duration = TimeSpan.FromHours(request.DurationHours) + TimeSpan.FromMinutes(request.DurationMinutes);

        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            StartDate = request.StartDate,
            Duration = duration,
            Status = request.Publish ? CompetitionStatus.Upcoming : CompetitionStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        db.Competitions.Add(competition);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Competition {Id} created by {UserId} (status={Status})",
            competition.Id, createdByUserId, competition.Status);

        return competition.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateCompetitionRequest request, Guid modifiedByUserId, CancellationToken ct = default)
    {
        var competition = await db.Competitions.FindAsync([id], ct)
            ?? throw new NotFoundException($"Compétition {id} introuvable.");

        var duration = TimeSpan.FromHours(request.DurationHours) + TimeSpan.FromMinutes(request.DurationMinutes);

        competition.Name = request.Name;
        competition.StartDate = request.StartDate;
        competition.Duration = duration;
        competition.LastModifiedByUserId = modifiedByUserId;
        competition.LastModifiedAt = DateTime.UtcNow;

        if (request.Publish && competition.Status == CompetitionStatus.Draft)
            competition.Status = CompetitionStatus.Upcoming;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Competition {Id} updated by {UserId}", id, modifiedByUserId);
    }
}
