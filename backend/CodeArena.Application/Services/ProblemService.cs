using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class ProblemService(
    IAppDbContext db,
    IFileStorageService fileStorage,
    IMarkdownSanitizerService markdownSanitizer,
    ILogger<ProblemService> logger) : IProblemService
{
    public async Task<ProblemDetailDto?> GetByIdAsync(Guid id, Guid? currentUserId, CancellationToken ct = default)
    {
        var problem = await db.Problems
            .Include(p => p.Competition)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (problem is null) return null;

        var totalSubmissions = await db.Submissions.CountAsync(s => s.ProblemId == id, ct);
        var acceptedSubmissions = await db.Submissions.CountAsync(
            s => s.ProblemId == id && s.Status == SubmissionStatus.Accepted, ct);
        var acceptanceRate = totalSubmissions > 0
            ? Math.Round((double)acceptedSubmissions / totalSubmissions * 100, 1)
            : 0.0;

        bool? solvedByCurrentUser = null;
        if (currentUserId.HasValue)
        {
            var status = await db.UserProblemStatuses
                .FirstOrDefaultAsync(ups => ups.ProblemId == id && ups.UserId == currentUserId.Value, ct);
            solvedByCurrentUser = status?.Solved ?? false;
        }

        return new ProblemDetailDto(
            problem.Id,
            problem.CompetitionId,
            problem.Competition!.Name,
            problem.Competition.Status.ToString(),
            problem.Title,
            problem.Body,
            problem.Points,
            totalSubmissions,
            acceptedSubmissions,
            acceptanceRate,
            solvedByCurrentUser
        );
    }

    public async Task<string?> GetInputFilePathAsync(Guid id, CancellationToken ct = default)
    {
        var problem = await db.Problems.FindAsync([id], ct);
        return problem?.InputFileUrl;
    }

    public async Task<Guid> CreateProblemAsync(
        Guid competitionId, CreateProblemRequest request, Guid createdByUserId,
        Stream inputStream, string inputFileName, Stream outputStream, string outputFileName,
        CancellationToken ct = default)
    {
        var competition = await db.Competitions.FindAsync([competitionId], ct)
            ?? throw new NotFoundException($"Compétition {competitionId} introuvable.");

        var inputFileUrl = await fileStorage.SaveFileAsync(inputStream, inputFileName, "inputs", ct);
        var outputFileUrl = await fileStorage.SaveFileAsync(outputStream, outputFileName, "outputs", ct);
        var sanitizedBody = markdownSanitizer.Sanitize(request.Body);

        var problem = new Problem
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            Title = request.Title,
            Body = sanitizedBody,
            Points = request.Points,
            InputFileUrl = inputFileUrl,
            OutputFileUrl = outputFileUrl,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        db.Problems.Add(problem);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Problem {Id} created in competition {CompId} by {UserId}",
            problem.Id, competitionId, createdByUserId);

        return problem.Id;
    }

    public async Task UpdateProblemAsync(
        Guid id, UpdateProblemRequest request, Guid modifiedByUserId,
        Stream? inputStream, string? inputFileName, Stream? outputStream, string? outputFileName,
        CancellationToken ct = default)
    {
        var problem = await db.Problems.FindAsync([id], ct)
            ?? throw new NotFoundException($"Exercice {id} introuvable.");

        problem.Title = request.Title;
        problem.Body = markdownSanitizer.Sanitize(request.Body);
        problem.Points = request.Points;
        problem.LastModifiedByUserId = modifiedByUserId;
        problem.LastModifiedAt = DateTime.UtcNow;

        if (request.ReplaceInputFile && inputStream is not null && inputFileName is not null)
            problem.InputFileUrl = await fileStorage.SaveFileAsync(inputStream, inputFileName, "inputs", ct);

        if (request.ReplaceOutputFile && outputStream is not null && outputFileName is not null)
            problem.OutputFileUrl = await fileStorage.SaveFileAsync(outputStream, outputFileName, "outputs", ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Problem {Id} updated by {UserId}", id, modifiedByUserId);
    }

    public async Task<ProblemEditFilesDto?> GetEditFilesAsync(Guid id, CancellationToken ct = default)
    {
        var problem = await db.Problems.FindAsync([id], ct);
        if (problem is null) return null;

        // Return relative URLs (served via /uploads/*) — OutputFileUrl intentionally included here
        // because this endpoint is [ModeratorOrAdmin] only.
        return new ProblemEditFilesDto(problem.InputFileUrl, problem.OutputFileUrl);
    }
}
