using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class SubmissionService(
    IAppDbContext db,
    IFileStorageService fileStorage,
    ILogger<SubmissionService> logger) : ISubmissionService
{
    public async Task<SubmitSolutionResult> SubmitAsync(
        Guid problemId,
        Guid userId,
        Stream resultFileStream,
        string resultFileName,
        Stream? sourceFileStream,
        string? sourceFileName,
        CancellationToken ct = default)
    {
        var problem = await db.Problems
            .Include(p => p.Competition)
            .FirstOrDefaultAsync(p => p.Id == problemId, ct)
            ?? throw new NotFoundException($"Problem {problemId} not found.");

        if (problem.Competition!.Status != CompetitionStatus.Ongoing)
            throw new InvalidOperationException("Submissions are only accepted while the competition is ongoing.");

        var existingStatus = await db.UserProblemStatuses
            .FirstOrDefaultAsync(ups => ups.ProblemId == problemId && ups.UserId == userId, ct);

        if (existingStatus?.Solved == true)
            throw new AlreadyAcceptedException();

        // Store uploaded files with GUID names (never the original filename)
        var resultRelativePath = await fileStorage.SaveFileAsync(resultFileStream, resultFileName, "submissions", ct);
        string? sourceRelativePath = null;
        if (sourceFileStream is not null && sourceFileName is not null)
            sourceRelativePath = await fileStorage.SaveFileAsync(sourceFileStream, sourceFileName, "submissions/src", ct);

        // Compare result with expected output (trim + normalize newlines per spec)
        var submittedContent = await fileStorage.ReadFileContentAsync(resultRelativePath, ct);
        var expectedContent = await fileStorage.ReadFileContentAsync(problem.OutputFileUrl, ct);
        var isAccepted = Normalize(submittedContent) == Normalize(expectedContent);
        var status = isAccepted ? SubmissionStatus.Accepted : SubmissionStatus.Wrong;

        // Check if this is the very first accepted submission on this problem (for leaderboard tie-breaker)
        var isFirstAccepted = false;
        if (isAccepted)
        {
            isFirstAccepted = !await db.Submissions
                .AnyAsync(s => s.ProblemId == problemId && s.Status == SubmissionStatus.Accepted, ct);
        }

        // Build the submission entity
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            UserId = userId,
            SubmittedAt = DateTime.UtcNow,
            ResultFileUrl = resultRelativePath,
            SourceFileUrl = sourceRelativePath,
            Status = status,
            IsFirstAccepted = isFirstAccepted
        };
        db.Submissions.Add(submission);

        // Upsert UserProblemStatus
        if (existingStatus is null)
        {
            db.UserProblemStatuses.Add(new UserProblemStatus
            {
                UserId = userId,
                ProblemId = problemId,
                Solved = isAccepted,
                AttemptCount = 1,
                LastAttemptAt = DateTime.UtcNow
            });
        }
        else
        {
            existingStatus.AttemptCount++;
            existingStatus.LastAttemptAt = DateTime.UtcNow;
            if (isAccepted) existingStatus.Solved = true;
        }

        // Update user total score in the same transaction
        if (isAccepted)
        {
            var user = await db.Users.FindAsync([userId], ct)
                ?? throw new NotFoundException($"User {userId} not found.");
            user.TotalScore += problem.Points;
        }

        // Single SaveChanges = implicit EF Core transaction (satisfies sprint rule)
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Submission {SubmissionId}: problem={ProblemId}, user={UserId}, status={Status}",
            submission.Id, problemId, userId, status);

        return isAccepted
            ? new SubmitSolutionResult("Accepted", $"Accepted ✓ — {problem.Points} points ajoutés à votre score", problem.Points)
            : new SubmitSolutionResult("Wrong", "Wrong Answer ✗ — Vérifiez les espaces et retours à la ligne", null);
    }

    public async Task<IEnumerable<SubmissionDto>> GetMySubmissionsAsync(
        Guid problemId, Guid userId, CancellationToken ct = default)
    {
        return await db.Submissions
            .Where(s => s.ProblemId == problemId && s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionDto(s.Id, s.SubmittedAt, s.Status.ToString(), s.IsFirstAccepted))
            .ToListAsync(ct);
    }

    private static string Normalize(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        var lines = content.ReplaceLineEndings("\n")
                           .Split('\n')
                           .Select(line => line.TrimEnd());
        return string.Join('\n', lines).Trim();
    }
}
