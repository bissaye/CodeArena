using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api")]
public class BadgesController(IBadgeService badgeService) : ControllerBase
{
    // GET /api/badges — catalogue complet (public)
    [HttpGet("badges")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var badges = await badgeService.GetAllBadgesAsync(ct);
        return Ok(badges);
    }

    // GET /api/users/{username}/badges — badges d'un utilisateur (public)
    [HttpGet("users/{username}/badges")]
    public async Task<IActionResult> GetUserBadges(string username, CancellationToken ct)
    {
        try
        {
            var badges = await badgeService.GetUserBadgesAsync(username, ct);
            return Ok(badges);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
