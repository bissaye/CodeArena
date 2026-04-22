using CodeArena.Application.DTOs;
using CodeArena.Domain.Enums;

namespace CodeArena.Application.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid userId, NotificationType type, string title, string body, CancellationToken ct = default);
    Task<NotificationsPageDto> GetPagedAsync(Guid userId, bool unreadOnly, int page, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}
