using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<ModeratorDto>> GetModeratorsAsync(CancellationToken ct = default);
    Task AddModeratorAsync(AddModeratorRequest request, CancellationToken ct = default);
    Task RemoveModeratorAsync(Guid userId, Guid requestingUserId, CancellationToken ct = default);
}
