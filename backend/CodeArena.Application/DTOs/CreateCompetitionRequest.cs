namespace CodeArena.Application.DTOs;

public class CreateCompetitionRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int DurationHours { get; set; }
    public int DurationMinutes { get; set; }
    public bool Publish { get; set; } = false;
}
