using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using OneMoi.Application.Common;

namespace OneMoi.Infrastructure.Services;

/// <summary>
/// Saves uploads (vendor logos) in Supabase Storage — free 1 GB, and it survives server restarts
/// (Render's free disk is wiped on every deploy / sleep).
/// Needs a PUBLIC bucket (default "onemoi") and the project's service_role key:
///   Supabase:Url        = https://YOUR-PROJECT.supabase.co
///   Supabase:ServiceKey = service_role key (Project Settings → API) — keep secret, server only
///   Supabase:Bucket     = onemoi
/// </summary>
public class SupabaseFileStorage(HttpClient http, IConfiguration config) : IFileStorage
{
    public async Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct = default)
    {
        var baseUrl = (config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured")).TrimEnd('/');
        var key = config["Supabase:ServiceKey"] ?? throw new InvalidOperationException("Supabase:ServiceKey is not configured");
        var bucket = config["Supabase:Bucket"] ?? "onemoi";

        var safeFolder = string.Join('/', folder.Split('/', '\\').Where(p => p.Length > 0 && p != ".." && p != "."));
        var path = $"{safeFolder}/{Path.GetFileName(fileName)}";

        using var body = new StreamContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", _ => "application/octet-stream"
        });
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/storage/v1/object/{bucket}/{path}") { Content = body };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Headers.Add("apikey", key);
        req.Headers.Add("x-upsert", "true");

        using var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            throw new AppException($"Could not store the file ({(int)res.StatusCode}). {await res.Content.ReadAsStringAsync(ct)}", 502);

        return $"{baseUrl}/storage/v1/object/public/{bucket}/{path}";
    }
}
