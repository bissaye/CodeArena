using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface IProblemService
{
    Task<ProblemDetailDto?> GetByIdAsync(Guid id, Guid? currentUserId, CancellationToken ct = default);
    Task<string?> GetInputFilePathAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateProblemAsync(Guid competitionId, CreateProblemRequest request, Guid createdByUserId,
        Stream inputStream, string inputFileName, Stream outputStream, string outputFileName, CancellationToken ct = default);
    Task UpdateProblemAsync(Guid id, UpdateProblemRequest request, Guid modifiedByUserId,
        Stream? inputStream, string? inputFileName, Stream? outputStream, string? outputFileName, CancellationToken ct = default);
    Task<ProblemEditFilesDto?> GetEditFilesAsync(Guid id, CancellationToken ct = default);
}
