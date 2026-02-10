using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api/competitions")]
public class CompetitionsController(
    ICompetitionService competitionService,
    IProblemService problemService,
    IValidator<CreateCompetitionRequest> createCompetitionValidator,
    IValidator<UpdateCompetitionRequest> updateCompetitionValidator,
    IValidator<CreateProblemRequest> createProblemValidator,
    ILogger<CompetitionsController> logger) : ControllerBase
{
    private static readonly string[] AllowedProblemFileExtensions = [".txt"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var canSeeDraft = User.IsInRole("Moderator") || User.IsInRole("Admin");
        var competitions = await competitionService.GetAllAsync(canSeeDraft, ct);
        return Ok(competitions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var canSeeDraft = User.IsInRole("Moderator") || User.IsInRole("Admin");
        var currentUserId = GetCurrentUserId();

        var competition = await competitionService.GetByIdAsync(id, canSeeDraft, currentUserId, ct);
        if (competition is null) return NotFound();
        return Ok(competition);
    }

    [HttpGet("{id:guid}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid id, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        if (top < 1 || top > 100) top = 10;
        var leaderboard = await competitionService.GetCompetitionLeaderboardAsync(id, top, ct);
        return Ok(leaderboard);
    }

    // US-08 — Créer une compétition
    [HttpPost]
    [Authorize(Policy = "ModeratorOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCompetitionRequest request, CancellationToken ct)
    {
        var validation = await createCompetitionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var userId = GetCurrentUserId()!.Value;
        var id = await competitionService.CreateAsync(request, userId, ct);

        logger.LogInformation("Competition {Id} created via API by {UserId}", id, userId);
        return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Compétition créée avec succès." });
    }

    // US-09 — Modifier une compétition
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ModeratorOrAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompetitionRequest request, CancellationToken ct)
    {
        var validation = await updateCompetitionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var userId = GetCurrentUserId()!.Value;

        try
        {
            await competitionService.UpdateAsync(id, request, userId, ct);
            return Ok(new { message = "Compétition mise à jour avec succès." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // US-12 — Ajouter un exercice à une compétition
    [HttpPost("{competitionId:guid}/problems")]
    [Authorize(Policy = "ModeratorOrAdmin")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    public async Task<IActionResult> CreateProblem(
        Guid competitionId,
        [FromForm] CreateProblemRequest request,
        IFormFile inputFile,
        IFormFile outputFile,
        CancellationToken ct)
    {
        var validation = await createProblemValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        if (!ValidateProblemFile(inputFile, out var inputError))
            return BadRequest(new { message = inputError });
        if (!ValidateProblemFile(outputFile, out var outputError))
            return BadRequest(new { message = outputError });

        var userId = GetCurrentUserId()!.Value;

        try
        {
            await using var inputStream = inputFile.OpenReadStream();
            await using var outputStream = outputFile.OpenReadStream();

            var problemId = await problemService.CreateProblemAsync(
                competitionId, request, userId,
                inputStream, inputFile.FileName,
                outputStream, outputFile.FileName,
                ct);

            logger.LogInformation("Problem {ProblemId} created in competition {CompId} via API", problemId, competitionId);
            return CreatedAtAction(
                "GetById",
                "Problems",
                new { id = problemId },
                new { id = problemId, message = "Exercice créé avec succès." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool ValidateProblemFile(IFormFile file, out string error)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedProblemFileExtensions.Contains(ext))
        {
            error = $"Le fichier '{file.FileName}' doit être un .txt.";
            return false;
        }
        if (file.Length > MaxFileSizeBytes)
        {
            error = $"Le fichier '{file.FileName}' dépasse la taille maximale de 5 Mo.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
