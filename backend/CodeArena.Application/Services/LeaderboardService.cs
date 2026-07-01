using System.Text.Json;
using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class LeaderboardService(
    IAppDbContext db,
    IDistributedCache cache,
    ILogger<LeaderboardService> logger) : ILeaderboardService
{
    private static readonly DistributedCacheEntryOptions CacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) };

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<IEnumerable<LeaderboardEntryDto>> GetGlobalLeaderboardAsync(int top, CancellationToken ct = default)
    {
        var cacheKey = $"leaderboard_global_{top}";
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            var cachedResult = JsonSerializer.Deserialize<List<LeaderboardEntryDto>>(cached, JsonOptions);
            if (cachedResult is not null)
                return cachedResult;
        }

        logger.LogDebug("Leaderboard cache miss — querying DB");

        var entries = await db.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.TotalScore)
            .Take(top)
            .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.Country, u.Region, u.TotalScore })
            .ToListAsync(ct);

        var result = entries
            .Select((u, i) => new LeaderboardEntryDto(i + 1, u.Id, u.Username, u.AvatarUrl, u.Country, u.Region, u.TotalScore, GetLevel(u.TotalScore)))
            .ToList();

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheOptions, ct);
        return result;
    }

    public async Task<LeaderboardPageDto> GetFilteredLeaderboardAsync(LeaderboardQueryDto query, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(query);
        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            var cachedPage = JsonSerializer.Deserialize<LeaderboardPageDto>(cached, JsonOptions);
            if (cachedPage is not null)
                return cachedPage;
        }

        logger.LogDebug("Filtered leaderboard cache miss — querying DB");

        var q = db.Users.Where(u => u.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Country))
            q = q.Where(u => u.Country == query.Country);

        if (!string.IsNullOrWhiteSpace(query.Region))
            q = q.Where(u => u.Region != null && u.Region.Contains(query.Region));

        if (!string.IsNullOrWhiteSpace(query.School))
            q = q.Where(u => u.School != null && u.School.Contains(query.School));

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(u => u.Username.Contains(query.Search));

        if (query.ScoreMin.HasValue)
            q = q.Where(u => u.TotalScore >= query.ScoreMin.Value);

        if (query.ScoreMax.HasValue)
            q = q.Where(u => u.TotalScore <= query.ScoreMax.Value);

        if (query.CompetitionOnly)
        {
            var activeCompetitionUserIds = await db.UserProblemStatuses
                .Where(ups => ups.Solved)
                .Join(db.Problems, ups => ups.ProblemId, p => p.Id, (ups, p) => new { ups.UserId, p.CompetitionId })
                .Join(db.Competitions, x => x.CompetitionId, c => c.Id, (x, c) => new { x.UserId, c.Status })
                .Where(x => x.Status != CompetitionStatus.Finished)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(ct);

            q = q.Where(u => activeCompetitionUserIds.Contains(u.Id));
        }

        if (query.CompetitionId.HasValue)
        {
            var competitionUserIds = await db.UserProblemStatuses
                .Where(ups => ups.Solved)
                .Join(db.Problems, ups => ups.ProblemId, p => p.Id, (ups, p) => new { ups.UserId, p.CompetitionId })
                .Where(x => x.CompetitionId == query.CompetitionId.Value)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(ct);

            q = q.Where(u => competitionUserIds.Contains(u.Id));
        }

        var total = await q.CountAsync(ct);

        var limit = Math.Clamp(query.Limit, 1, 100);
        var offset = Math.Max(query.Offset, 0);

        var users = await q
            .OrderByDescending(u => u.TotalScore)
            .ThenBy(u => u.Username)
            .Skip(offset)
            .Take(limit)
            .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.Country, u.Region, u.TotalScore })
            .ToListAsync(ct);

        var entries = users
            .Select((u, i) => new LeaderboardEntryDto(offset + i + 1, u.Id, u.Username, u.AvatarUrl, u.Country, u.Region, u.TotalScore, GetLevel(u.TotalScore)))
            .ToList();

        var page = new LeaderboardPageDto(total, offset, limit, DateTime.UtcNow, entries);
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(page), CacheOptions, ct);
        return page;
    }

    private static string BuildCacheKey(LeaderboardQueryDto q) =>
        $"leaderboard_filtered|{q.Country}|{q.Region}|{q.School}|{q.CompetitionId}|{q.ScoreMin}|{q.ScoreMax}|{q.CompetitionOnly}|{q.Search}|{q.Offset}|{q.Limit}";

    private static string GetLevel(int score) => score switch
    {
        >= 1500 => "Expert",
        >= 500  => "Avancé",
        >= 100  => "Intermédiaire",
        _       => "Débutant"
    };
}
