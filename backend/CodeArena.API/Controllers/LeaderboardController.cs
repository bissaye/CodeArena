using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController(ILeaderboardService leaderboardService) : ControllerBase
{
    // Mini leaderboard (page d'accueil / sidebar)
    [HttpGet("mini")]
    public async Task<IActionResult> GetMini([FromQuery] int top = 20, CancellationToken ct = default)
    {
        if (top < 1 || top > 100) top = 20;
        var entries = await leaderboardService.GetGlobalLeaderboardAsync(top, ct);
        return Ok(entries);
    }

    // Leaderboard global filtré avec pagination
    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] string? country = null,
        [FromQuery] string? region = null,
        [FromQuery] string? school = null,
        [FromQuery] Guid? competitionId = null,
        [FromQuery] int? scoreMin = null,
        [FromQuery] int? scoreMax = null,
        [FromQuery] bool competitionOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var query = new LeaderboardQueryDto
        {
            Country = country,
            Region = region,
            School = school,
            CompetitionId = competitionId,
            ScoreMin = scoreMin,
            ScoreMax = scoreMax,
            CompetitionOnly = competitionOnly,
            Search = search,
            Offset = offset,
            Limit = Math.Clamp(limit, 1, 100)
        };

        var page = await leaderboardService.GetFilteredLeaderboardAsync(query, ct);
        return Ok(page);
    }
}
