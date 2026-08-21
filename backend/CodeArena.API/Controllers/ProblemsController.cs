using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeArena.API.Controllers;

[ApiController]
[Route("api/problems")]
public class ProblemsController(
    IProblemService problemService,
    ISubmissionService submissionService,
    IFileStorageService fileStorage,
    IBadgeService badgeService,
    IValidator<UpdateProblemRequest> updateProblemValidator,
    ILogger<ProblemsController> logger) : ControllerBase
{
    private static readonly string[] AllowedResultExtensions = [".txt"];
    private static readonly string[] AllowedSourceExtensions = [".c", ".cpp", ".py", ".java", ".js"];
    private static readonly string[] AllowedProblemFileExtensions = [".txt"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // GET /api/problems/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var problem = await problemService.GetByIdAsync(id, currentUserId, ct);
        if (problem is null) return NotFound();
        return Ok(problem);
    }

    // GET /api/problems/{id}/input
    [HttpGet("{id:guid}/input")]
    public async Task<IActionResult> GetInput(Guid id, CancellationToken ct)
    {
        var filePath = await problemService.GetInputFilePathAsync(id, ct);
        if (filePath is null) return NotFound();

        var absolutePath = fileStorage.GetAbsolutePath(filePath);
        if (!System.IO.File.Exists(absolutePath))
        {
            logger.LogWarning("Input file not found on disk for problem {ProblemId}: {Path}", id, absolutePath);
            return NotFound("Input file not found on server.");
        }

        // Track download for speed-solver badge (authenticated users only, awaited before streaming)
        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
            await badgeService.RecordInputDownloadAsync(currentUserId.Value, id, ct);

        var stream = System.IO.File.OpenRead(absolutePath);
        return File(stream, "text/plain", "input.txt");
    }

    // GET /api/problems/{id}/input-edit — [ModeratorOrAdmin] retourne les 2 URLs de fichiers
    [HttpGet("{id:guid}/input-edit")]
    [Authorize(Policy = "ModeratorOrAdmin")]
    public async Task<IActionResult> GetEditFiles(Guid id, CancellationToken ct)
    {
        var files = await problemService.GetEditFilesAsync(id, ct);
        if (files is null) return NotFound();
        return Ok(files);
    }

    // US-13 — Modifier un exercice
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ModeratorOrAdmin")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    public async Task<IActionResult> UpdateProblem(
        Guid id,
        [FromForm] UpdateProblemRequest request,
        IFormFile? inputFile,
        IFormFile? outputFile,
        CancellationToken ct)
    {
        var validation = await updateProblemValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        if (request.ReplaceInputFile && inputFile is not null)
        {
            if (!ValidateProblemFile(inputFile, out var inputError))
                return BadRequest(new { message = inputError });
        }
        if (request.ReplaceOutputFile && outputFile is not null)
        {
            if (!ValidateProblemFile(outputFile, out var outputError))
                return BadRequest(new { message = outputError });
        }

        var userId = GetCurrentUserId()!.Value;

        try
        {
            await using var inputStream = inputFile?.OpenReadStream();
            await using var outputStream = outputFile?.OpenReadStream();

            await problemService.UpdateProblemAsync(
                id, request, userId,
                inputStream, inputFile?.FileName,
                outputStream, outputFile?.FileName,
                ct);

            return Ok(new { message = "Exercice mis à jour avec succès." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/problems/{id}/submit
    [HttpPost("{id:guid}/submit")]
    [Authorize]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    public async Task<IActionResult> Submit(
        Guid id,
        IFormFile resultFile,
        IFormFile? sourceFile,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var resultExt = Path.GetExtension(resultFile.FileName).ToLowerInvariant();
        if (!AllowedResultExtensions.Contains(resultExt))
            return BadRequest("Le fichier résultat doit être un .txt.");
        if (resultFile.Length > MaxFileSizeBytes)
            return BadRequest("Le fichier résultat dépasse la taille maximale de 5 Mo.");
        if (!resultFile.ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            && resultFile.ContentType != "application/octet-stream")
            return BadRequest("Type MIME invalide pour le fichier résultat.");

        if (sourceFile is not null)
        {
            var sourceExt = Path.GetExtension(sourceFile.FileName).ToLowerInvariant();
            if (!AllowedSourceExtensions.Contains(sourceExt))
                return BadRequest($"Extension source non supportée. Autorisées : {string.Join(", ", AllowedSourceExtensions)}");
            if (sourceFile.Length > MaxFileSizeBytes)
                return BadRequest("Le fichier source dépasse la taille maximale de 5 Mo.");
        }

        try
        {
            await using var resultStream = resultFile.OpenReadStream();
            await using var sourceStream = sourceFile?.OpenReadStream();

            var result = await submissionService.SubmitAsync(
                id, userId.Value,
                resultStream, resultFile.FileName,
                sourceStream, sourceFile?.FileName,
                ct);

            return Ok(result);
        }
        catch (AlreadyAcceptedException)
        {
            return Conflict(new { message = "Vous avez déjà résolu cet exercice." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/problems/{id}/submissions/me
    [HttpGet("{id:guid}/submissions/me")]
    [Authorize]
    public async Task<IActionResult> GetMySubmissions(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var submissions = await submissionService.GetMySubmissionsAsync(id, userId.Value, ct);
        return Ok(submissions);
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
