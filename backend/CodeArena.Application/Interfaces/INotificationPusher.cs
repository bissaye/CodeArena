using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface INotificationPusher
{
    Task PushAsync(Guid userId, NotificationDto dto, CancellationToken ct = default);
}
