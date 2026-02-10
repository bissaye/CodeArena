using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface ISubmissionService
{
    Task<SubmitSolutionResult> SubmitAsync(
        Guid problemId,
        Guid userId,
        Stream resultFileStream,
        string resultFileName,
        Stream? sourceFileStream,
        string? sourceFileName,
        CancellationToken ct = default);

    Task<IEnumerable<SubmissionDto>> GetMySubmissionsAsync(
        Guid problemId,
        Guid userId,
        CancellationToken ct = default);
}
