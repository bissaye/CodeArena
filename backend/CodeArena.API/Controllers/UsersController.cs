using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(
    IUserService userService,
    IValidator<UpdateUserRequest> updateValidator,
    ILogger<UsersController> logger) : ControllerBase
{
    private const long MaxAvatarSizeBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedAvatarExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedAvatarMimeTypes = ["image/jpeg", "image/png"];

    // GET /api/users/{username}
    [HttpGet("{username}")]
    public async Task<IActionResult> GetProfile(string username, CancellationToken ct)
    {
        try
        {
            var profile = await userService.GetProfileAsync(username, ct);
            return Ok(profile);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // PUT /api/users/{username}
    [HttpPut("{username}")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        string username, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            await userService.UpdateProfileAsync(username, userId.Value, request, ct);
            return Ok(new { message = "Profil mis à jour avec succès." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    // PUT /api/users/{username}/avatar
    [HttpPut("{username}/avatar")]
    [Authorize]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(
        string username, IFormFile avatarFile, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Validate before calling service (fail fast)
        var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
        if (!AllowedAvatarExtensions.Contains(ext))
            return BadRequest(new { message = "Formats acceptés : JPG, PNG." });

        if (avatarFile.Length > MaxAvatarSizeBytes)
            return BadRequest(new { message = "L'avatar ne doit pas dépasser 2 Mo." });

        if (!AllowedAvatarMimeTypes.Contains(avatarFile.ContentType.ToLowerInvariant()))
            return BadRequest(new { message = "Type MIME invalide. Formats acceptés : image/jpeg, image/png." });

        try
        {
            await using var stream = avatarFile.OpenReadStream();
            var relativePath = await userService.UploadAvatarAsync(
                username, userId.Value,
                stream, avatarFile.FileName, avatarFile.ContentType, avatarFile.Length,
                ct);

            logger.LogInformation("Avatar uploadé pour {Username} : {Path}", username, relativePath);
            return Ok(new { avatarUrl = relativePath });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/users/{username}/submissions
    [HttpGet("{username}/submissions")]
    public async Task<IActionResult> GetUserSubmissions(string username, CancellationToken ct)
    {
        try
        {
            var profile = await userService.GetProfileAsync(username, ct);
            return Ok(new { recentActivity = profile.RecentActivity });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/users/regions
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions(CancellationToken ct)
    {
        var regions = await userService.GetRegionsAsync(ct);
        return Ok(regions);
    }

    // GET /api/users/schools
    [HttpGet("schools")]
    public async Task<IActionResult> GetSchools(CancellationToken ct)
    {
        var schools = await userService.GetSchoolsAsync(ct);
        return Ok(schools);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
