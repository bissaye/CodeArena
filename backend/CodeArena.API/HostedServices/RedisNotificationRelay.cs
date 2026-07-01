using System.Text.Json;
using CodeArena.API.Hubs;
using CodeArena.Application.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CodeArena.API.HostedServices;

// Subscribes to Redis pub/sub channel and relays notifications to connected SignalR clients.
// This decouples the Worker process (which creates notifications) from the API process (which holds WebSocket connections).
public class RedisNotificationRelay(
    IConnectionMultiplexer redis,
    IHubContext<NotificationHub> hubContext,
    ILogger<RedisNotificationRelay> logger) : IHostedService
{
    private ISubscriber? _subscriber;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscriber = redis.GetSubscriber();

        await _subscriber.SubscribeAsync(
            RedisChannel.Pattern("notifications:push:*"),
            async (channel, message) =>
            {
                if (!message.HasValue) return;

                try
                {
                    // Channel format: "notifications:push:{userId}"
                    var channelStr = channel.ToString();
                    var userId = channelStr["notifications:push:".Length..];

                    var dto = JsonSerializer.Deserialize<NotificationDto>(message.ToString(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto is not null)
                        await hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", dto);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error relaying notification from Redis to SignalR");
                }
            });

        logger.LogInformation("RedisNotificationRelay started — subscribed to notifications:push:*");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscriber is not null)
            await _subscriber.UnsubscribeAllAsync();
    }
}
