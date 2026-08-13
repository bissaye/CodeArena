namespace CodeArena.Application.DTOs;

public record LeaderboardUpdateEvent(
    Guid CompetitionId,
    string Username,
    int NewScore,
    int Rank);
