using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class AdminService(IAppDbContext db, ILogger<AdminService> logger) : IAdminService
{
    public async Task<IEnumerable<ModeratorDto>> GetModeratorsAsync(CancellationToken ct = default)
    {
        var moderators = await db.Users
            .Where(u => u.Role == UserRole.Moderator && u.IsActive)
            .OrderBy(u => u.Username)
            .Select(u => new ModeratorDto(u.Id, u.Username, u.AvatarUrl, u.PromotedAt))
            .ToListAsync(ct);

        return moderators;
    }

    public async Task AddModeratorAsync(AddModeratorRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);

        if (user is null)
            throw new NotFoundException($"Utilisateur '{request.Username}' introuvable.");

        if (user.Role == UserRole.Admin)
            throw new ConflictException("Impossible de modifier le rôle d'un administrateur.");

        if (user.Role == UserRole.Moderator)
            throw new ConflictException($"'{request.Username}' est déjà modérateur.");

        user.Role = UserRole.Moderator;
        user.PromotedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} promoted to Moderator", request.Username);
    }

    public async Task RemoveModeratorAsync(Guid userId, Guid requestingUserId, CancellationToken ct = default)
    {
        if (userId == requestingUserId)
            throw new BadRequestException("Vous ne pouvez pas vous retirer vous-même.");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

        if (user is null || user.Role != UserRole.Moderator)
            throw new NotFoundException("Modérateur introuvable.");

        user.Role = UserRole.Participant;
        user.PromotedAt = null;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} ({UserId}) demoted from Moderator", user.Username, userId);
    }
}
