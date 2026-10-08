using Microsoft.Extensions.Configuration;
using OneMoi.Application.Common;

namespace OneMoi.Infrastructure.Services;

/// <summary>Saves uploads under {Storage:Root}/uploads/… and returns the public URL path.
/// Swap for Azure Blob / S3 when hosting.</summary>
public class LocalFileStorage(IConfiguration config) : IFileStorage
{
    public async Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct = default)
    {
        var root = config["Storage:Root"] ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var safeFolder = string.Join('/', folder.Split('/', '\\').Where(p => p.Length > 0 && p != ".." && p != "."));
        var safeName = Path.GetFileName(fileName);
        var dir = Path.Combine(root, "uploads", safeFolder);
        Directory.CreateDirectory(dir);
        await using (var fs = File.Create(Path.Combine(dir, safeName)))
            await content.CopyToAsync(fs, ct);
        return $"/uploads/{safeFolder}/{safeName}";
    }
}
