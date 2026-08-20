using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface ICompetitionService
{
    Task<IEnumerable<CompetitionSummaryDto>> GetAllAsync(bool canSeeDraft, CancellationToken ct = default);
    Task<CompetitionDetailDto?> GetByIdAsync(Guid id, bool canSeeDraft, Guid? currentUserId, CancellationToken ct = default);
    Task<IEnumerable<LeaderboardEntryDto>> GetCompetitionLeaderboardAsync(Guid competitionId, int top, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateCompetitionRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateCompetitionRequest request, Guid modifiedByUserId, CancellationToken ct = default);
}
