using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface IBadgeService
{
    Task CheckAndAwardBadgesAsync(Guid userId, Guid problemId, CancellationToken ct = default);
    Task CheckAndAwardMentorBadgeAsync(Guid problemCreatorId, CancellationToken ct = default);
    Task RecordInputDownloadAsync(Guid userId, Guid problemId, CancellationToken ct = default);
    Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(string username, CancellationToken ct = default);
    Task<IEnumerable<BadgeDto>> GetAllBadgesAsync(CancellationToken ct = default);
}
