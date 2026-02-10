using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(
    IAdminService adminService,
    IValidator<AddModeratorRequest> validator) : ControllerBase
{
    // GET /api/admin/moderators
    [HttpGet("moderators")]
    public async Task<IActionResult> GetModerators(CancellationToken ct)
    {
        var moderators = await adminService.GetModeratorsAsync(ct);
        return Ok(moderators);
    }

    // POST /api/admin/moderators
    [HttpPost("moderators")]
    public async Task<IActionResult> AddModerator([FromBody] AddModeratorRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { message = validation.Errors[0].ErrorMessage });

        try
        {
            await adminService.AddModeratorAsync(request, ct);
            return Created(string.Empty, new { message = $"'{request.Username}' est maintenant modérateur." });
        }
        catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ConflictException ex) { return Conflict(new { message = ex.Message }); }
    }

    // DELETE /api/admin/moderators/{userId}
    [HttpDelete("moderators/{userId:guid}")]
    public async Task<IActionResult> RemoveModerator(Guid userId, CancellationToken ct)
    {
        var requestingUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await adminService.RemoveModeratorAsync(userId, requestingUserId, ct);
            return Ok(new { message = "Modérateur retiré avec succès." });
        }
        catch (BadRequestException ex) { return BadRequest(new { message = ex.Message }); }
        catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
