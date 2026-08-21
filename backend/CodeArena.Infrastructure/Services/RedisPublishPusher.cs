using System.Text.Json;
using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CodeArena.Infrastructure.Services;

public class RedisPublishPusher(
    IConnectionMultiplexer redis,
    ILogger<RedisPublishPusher> logger) : INotificationPusher
{
    public async Task PushAsync(Guid userId, NotificationDto dto, CancellationToken ct = default)
    {
        try
        {
            var channel = RedisChannel.Literal($"notifications:push:{userId}");
            var payload = JsonSerializer.Serialize(dto);
            await redis.GetSubscriber().PublishAsync(channel, payload);
            logger.LogDebug("Pushed notification to Redis for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to push notification to Redis for user {UserId}", userId);
        }
    }
}
