namespace CodeArena.Application.DTOs;

public record UserProfileDto(
    string Username,
    string? AvatarUrl,
    string Country,
    string? Region,
    string? School,
    int TotalScore,
    int CompetitionScore,
    int NationalRank,
    DateTime CreatedAt,
    IReadOnlyList<UserActivityDto> RecentActivity
);
