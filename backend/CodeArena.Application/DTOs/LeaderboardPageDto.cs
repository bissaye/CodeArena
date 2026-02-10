namespace CodeArena.Application.DTOs;

public record LeaderboardPageDto(
    int Total,
    int Offset,
    int Limit,
    DateTime RefreshedAt,
    IReadOnlyList<LeaderboardEntryDto> Entries
);
