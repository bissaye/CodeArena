namespace CodeArena.Application.Interfaces;

public interface ILeaderboardBroadcastService
{
    Task BroadcastUpdateAsync(Guid userId, Guid competitionId, CancellationToken ct);
}
