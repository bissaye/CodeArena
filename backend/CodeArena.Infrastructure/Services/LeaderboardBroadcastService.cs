using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Infrastructure.Services;

public class LeaderboardBroadcastService(
    IAppDbContext db,
    ILeaderboardPusher pusher) : ILeaderboardBroadcastService
{
    public async Task BroadcastUpdateAsync(Guid userId, Guid competitionId, CancellationToken ct)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.TotalScore })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        var rank = await db.Users.CountAsync(u => u.TotalScore > user.TotalScore, ct) + 1;

        await pusher.BroadcastAsync(new LeaderboardUpdateEvent(
            competitionId,
            user.Username,
            user.TotalScore,
            rank), ct);
    }
}
