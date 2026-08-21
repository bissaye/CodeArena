namespace CodeArena.Application.DTOs;

public record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string Username,
    string? AvatarUrl,
    string Country,
    string? Region,
    int Score,
    string Level
);
