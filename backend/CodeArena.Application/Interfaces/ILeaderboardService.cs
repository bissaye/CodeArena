using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface ILeaderboardService
{
    Task<IEnumerable<LeaderboardEntryDto>> GetGlobalLeaderboardAsync(int top, CancellationToken ct = default);
    Task<LeaderboardPageDto> GetFilteredLeaderboardAsync(LeaderboardQueryDto query, CancellationToken ct = default);
}
