namespace CodeArena.Application.DTOs;

public record ModeratorDto(
    Guid UserId,
    string Username,
    string? AvatarUrl,
    DateTime? PromotedAt
);
