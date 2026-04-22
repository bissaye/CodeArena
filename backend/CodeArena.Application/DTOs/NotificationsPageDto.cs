namespace CodeArena.Application.DTOs;

public record NotificationsPageDto(
    int Total,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<NotificationDto> Items
);
