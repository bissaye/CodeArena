namespace CodeArena.Application.DTOs;

public class UpdateProblemRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool ReplaceInputFile { get; set; } = false;
    public bool ReplaceOutputFile { get; set; } = false;
}
