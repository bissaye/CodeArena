using CodeArena.Application.DTOs;
using CodeArena.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(
    INotificationService notificationService) : ControllerBase
{
    // GET /api/notifications?unreadOnly=false&page=1
    [HttpGet]
    [ProducesResponseType(typeof(NotificationsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await notificationService.GetPagedAsync(userId.Value, unreadOnly, page, ct);
        return Ok(result);
    }

    // PUT /api/notifications/{id}/read
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await notificationService.MarkAsReadAsync(id, userId.Value, ct);
        return NoContent();
    }

    // PUT /api/notifications/read-all
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await notificationService.MarkAllAsReadAsync(userId.Value, ct);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
