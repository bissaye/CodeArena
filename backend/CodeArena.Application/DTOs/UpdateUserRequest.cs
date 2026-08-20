namespace CodeArena.Application.DTOs;

public class UpdateUserRequest
{
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? School { get; set; }
}
