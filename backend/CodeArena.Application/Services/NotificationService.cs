using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class NotificationService(
    IAppDbContext db,
    INotificationPusher pusher,
    ILogger<NotificationService> logger) : INotificationService
{
    private const int PageSize = 20;

    public async Task CreateAsync(Guid userId, NotificationType type, string title, string body, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Notification created: userId={UserId} type={Type}", userId, type);

        // Push real-time via Redis → SignalR relay in API process
        var dto = new NotificationDto(notification.Id, type.ToString(), title, body, false, notification.CreatedAt, null);
        await pusher.PushAsync(userId, dto, ct);
    }

    public async Task<NotificationsPageDto> GetPagedAsync(Guid userId, bool unreadOnly, int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var query = db.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(ct);
        var unreadCount = await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
        var totalPages = (int)Math.Ceiling((double)total / PageSize);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Title, n.Body, n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

        return new NotificationsPageDto(total, unreadCount, page, PageSize, totalPages, items);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null || notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (unread.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unread.Count, userId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }
}
