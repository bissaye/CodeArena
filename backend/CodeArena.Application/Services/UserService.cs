using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class UserService(
    IAppDbContext db,
    IFileStorageService fileStorage,
    ILogger<UserService> logger) : IUserService
{
    private static readonly string[] AllowedAvatarExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedAvatarMimeTypes = ["image/jpeg", "image/png"];
    private const long MaxAvatarSizeBytes = 2 * 1024 * 1024; // 2 MB

    public async Task<UserProfileDto> GetProfileAsync(string username, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct)
            ?? throw new NotFoundException($"Utilisateur '{username}' introuvable.");

        // National rank = position parmi les utilisateurs actifs triés par TotalScore décroissant
        var nationalRank = await db.Users
            .CountAsync(u => u.IsActive && u.Country == user.Country && u.TotalScore > user.TotalScore, ct) + 1;

        // CompetitionScore = score des compétitions en cours (Ongoing uniquement)
        var competitionScore = await db.UserProblemStatuses
            .Where(ups => ups.UserId == user.Id && ups.Solved)
            .Join(db.Problems, ups => ups.ProblemId, p => p.Id, (ups, p) => p)
            .Join(db.Competitions, p => p.CompetitionId, c => c.Id, (p, c) => new { p.Points, c.Status })
            .Where(x => x.Status == Domain.Enums.CompetitionStatus.Ongoing)
            .SumAsync(x => x.Points, ct);

        // 20 dernières soumissions (activité récente)
        var recentActivity = await db.Submissions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(20)
            .Join(db.Problems, s => s.ProblemId, p => p.Id, (s, p) => new { s, p })
            .Join(db.Competitions, x => x.p.CompetitionId, c => c.Id, (x, c) => new UserActivityDto(
                x.p.Id,
                x.p.Title,
                c.Id,
                c.Name,
                x.s.Status.ToString(),
                x.s.SubmittedAt))
            .ToListAsync(ct);

        return new UserProfileDto(
            user.Username,
            user.AvatarUrl,
            user.Country,
            user.Region,
            user.School,
            user.TotalScore,
            competitionScore,
            nationalRank,
            user.CreatedAt,
            recentActivity);
    }

    public async Task UpdateProfileAsync(
        string username, Guid requesterId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct)
            ?? throw new NotFoundException($"Utilisateur '{username}' introuvable.");

        if (user.Id != requesterId)
            throw new ForbiddenException("Vous ne pouvez modifier que votre propre profil.");

        user.Country = request.Country;
        user.Region = request.Region;
        user.School = request.School;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Profil mis à jour : {Username}", username);
    }

    public async Task<string> UploadAvatarAsync(
        string username, Guid requesterId,
        Stream fileStream, string fileName, string contentType, long fileLength,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct)
            ?? throw new NotFoundException($"Utilisateur '{username}' introuvable.");

        if (user.Id != requesterId)
            throw new ForbiddenException("Vous ne pouvez modifier que votre propre profil.");

        // Validate size
        if (fileLength > MaxAvatarSizeBytes)
            throw new ArgumentException("L'avatar ne doit pas dépasser 2 Mo.");

        // Validate extension + MIME type
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedAvatarExtensions.Contains(ext))
            throw new ArgumentException("Formats acceptés : JPG, PNG.");
        if (!AllowedAvatarMimeTypes.Contains(contentType.ToLowerInvariant()))
            throw new ArgumentException("Type MIME invalide. Formats acceptés : image/jpeg, image/png.");

        // Save with resize 200×200 (delegated to Infrastructure with SixLabors.ImageSharp)
        var relativePath = await fileStorage.SaveAvatarAsync(fileStream, ct);

        user.AvatarUrl = relativePath;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Avatar mis à jour : {Username} → {Path}", username, relativePath);
        return relativePath;
    }

    public async Task<IEnumerable<string>> GetRegionsAsync(CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => u.Region != null && u.Region != "")
            .Select(u => u.Region!)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetSchoolsAsync(CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => u.School != null && u.School != "")
            .Select(u => u.School!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);
    }
}
