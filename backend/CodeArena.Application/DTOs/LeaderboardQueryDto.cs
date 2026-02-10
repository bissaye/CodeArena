namespace CodeArena.Application.DTOs;

public class LeaderboardQueryDto
{
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? School { get; set; }
    public Guid? CompetitionId { get; set; }
    public int? ScoreMin { get; set; }
    public int? ScoreMax { get; set; }
    public bool CompetitionOnly { get; set; } = false;
    public string? Search { get; set; }
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 50;
}
