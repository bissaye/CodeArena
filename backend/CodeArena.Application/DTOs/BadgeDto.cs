namespace CodeArena.Application.DTOs;

public record BadgeDto(
    Guid Id,
    string Slug,
    string Name,
    string Description,
    string IconUrl,
    DateTime? EarnedAt
);
