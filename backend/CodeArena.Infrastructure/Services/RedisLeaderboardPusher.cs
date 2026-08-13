using System.Text.Json;
using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using StackExchange.Redis;

namespace CodeArena.Infrastructure.Services;

public class RedisLeaderboardPusher(IConnectionMultiplexer redis) : ILeaderboardPusher
{
    public async Task BroadcastAsync(LeaderboardUpdateEvent evt, CancellationToken ct)
    {
        var sub = redis.GetSubscriber();
        var json = JsonSerializer.Serialize(evt);
        await sub.PublishAsync(RedisChannel.Literal("leaderboard:updated"), json);
    }
}
