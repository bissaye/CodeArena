using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface ILeaderboardPusher
{
    Task BroadcastAsync(LeaderboardUpdateEvent evt, CancellationToken ct);
}
