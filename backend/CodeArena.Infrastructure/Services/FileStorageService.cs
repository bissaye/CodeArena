using CodeArena.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CodeArena.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _contentRootPath;
    private readonly string _uploadsAbsolutePath;

    public FileStorageService(IConfiguration configuration, IHostEnvironment env)
    {
        _contentRootPath = env.ContentRootPath;
        var configured = configuration["UPLOADS_PATH"];
        _uploadsAbsolutePath = !string.IsNullOrEmpty(configured) && Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(_contentRootPath, "uploads");
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream, string originalFileName, string subfolder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var subfolderPath = Path.Combine(_uploadsAbsolutePath, subfolder);
        Directory.CreateDirectory(subfolderPath);
        var fullPath = Path.Combine(subfolderPath, fileName);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(fs, ct);

        // Store as relative path (consistent with seed data convention: "uploads/...")
        return $"uploads/{subfolder}/{fileName}";
    }

    public async Task<string> SaveAvatarAsync(Stream imageStream, CancellationToken ct = default)
    {
        var avatarDir = Path.Combine(_uploadsAbsolutePath, "avatars");
        Directory.CreateDirectory(avatarDir);

        var fileName = $"{Guid.NewGuid()}.jpg";
        var fullPath = Path.Combine(avatarDir, fileName);

        using var image = await Image.LoadAsync(imageStream, ct);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(200, 200),
            Mode = ResizeMode.Crop
        }));

        await image.SaveAsJpegAsync(fullPath, ct);

        return $"uploads/avatars/{fileName}";
    }

    public async Task<string> ReadFileContentAsync(string relativeFilePath, CancellationToken ct = default) =>
        await File.ReadAllTextAsync(GetAbsolutePath(relativeFilePath), ct);

    public string GetAbsolutePath(string relativeFilePath) =>
        Path.Combine(_contentRootPath, relativeFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
}
