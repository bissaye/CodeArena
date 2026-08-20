namespace CodeArena.Application.DTOs;

public class CreateProblemRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Points { get; set; }
}
