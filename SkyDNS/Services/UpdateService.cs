using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkyDNS;

public record UpdateInfo(Version Version, string Url, long Size);

/// <summary>Updates from GitHub Releases: tag "vX.Y.Z" with a "SkyDNS-Setup.exe" asset (Inno Setup installer).</summary>
public static class UpdateService
{
    const string Api = "https://api.github.com/repos/aryankholqi/skydns/releases/latest";
    const string DownloadPrefix = "https://github.com/aryankholqi/skydns/releases/download/";
    const string Asset = "SkyDNS-Setup.exe";
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30), DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue("SkyDNS", "1") } } };

    static Version Norm(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build)); // 1.2.0.0 == 1.2.0
    public static Version Current => Norm(typeof(UpdateService).Assembly.GetName().Version!);

    static string Dir => Path.GetDirectoryName(Environment.ProcessPath!)!;
    // the app runs unelevated and can't write to Program Files; the installer elevates itself via UAC
    static string CacheDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkyDNS");
    static string SetupPath => Path.Combine(CacheDir, Asset);

    /// <summary>Newer release, or null when up to date. Throws on network/API errors.</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        using var doc = JsonDocument.Parse(await Http.GetStringAsync(Api));
        var r = doc.RootElement;
        if (!Version.TryParse(r.GetProperty("tag_name").GetString()?.TrimStart('v', 'V'), out var v) || Norm(v) <= Current) return null;
        foreach (var a in r.GetProperty("assets").EnumerateArray())
            if (a.GetProperty("name").GetString() == Asset && a.GetProperty("browser_download_url").GetString() is { } url && url.StartsWith(DownloadPrefix))
                return new(Norm(v), url, a.GetProperty("size").GetInt64());
        return null;
    }

    /// <summary>Downloads the installer and returns its path. The caller runs it and exits.</summary>
    public static async Task<string> DownloadAsync(UpdateInfo u, IProgress<double> progress)
    {
        var path = SetupPath;
        Directory.CreateDirectory(CacheDir);
        try
        {
            using var res = await Http.GetAsync(u.Url, HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();
            await using (var src = await res.Content.ReadAsStreamAsync())
            await using (var dst = File.Create(path))
            {
                var buf = new byte[1 << 16]; long done = 0; int n;
                while ((n = await src.ReadAsync(buf)) > 0)
                {
                    await dst.WriteAsync(buf.AsMemory(0, n));
                    progress.Report((double)(done += n) / u.Size);
                }
            }
            if (new FileInfo(path).Length != u.Size) throw new IOException(Loc.I["updBroken"]);
            return path;
        }
        catch { try { File.Delete(path); } catch { } throw; }
    }

    /// <summary>Remove the installer from the last update, and the exe left by 1.2.0's in-place swap.</summary>
    public static void Cleanup()
    {
        foreach (var f in new[] { SetupPath, Path.Combine(Dir, Asset), Path.Combine(Dir, "SkyDNS.old.exe") })
            try { File.Delete(f); } catch { }
    }
}
