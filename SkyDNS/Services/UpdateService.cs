using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkyDNS;

public record UpdateInfo(Version Version, string Url, long Size);

/// <summary>Updates from GitHub Releases: tag "vX.Y.Z" with a "SkyDNS.exe" asset.</summary>
public static class UpdateService
{
    const string Api = "https://api.github.com/repos/aryankholqi/skydns/releases/latest";
    const string DownloadPrefix = "https://github.com/aryankholqi/skydns/releases/download/";
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30), DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue("SkyDNS", "1") } } };

    static Version Norm(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build)); // 1.2.0.0 == 1.2.0
    public static Version Current => Norm(typeof(UpdateService).Assembly.GetName().Version!);

    static string Dir => Path.GetDirectoryName(Environment.ProcessPath!)!;
    static string OldExe => Path.Combine(Dir, "SkyDNS.old.exe");

    /// <summary>Newer release, or null when up to date. Throws on network/API errors.</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        using var doc = JsonDocument.Parse(await Http.GetStringAsync(Api));
        var r = doc.RootElement;
        if (!Version.TryParse(r.GetProperty("tag_name").GetString()?.TrimStart('v', 'V'), out var v) || Norm(v) <= Current) return null;
        foreach (var a in r.GetProperty("assets").EnumerateArray())
            if (a.GetProperty("name").GetString() == "SkyDNS.exe" && a.GetProperty("browser_download_url").GetString() is { } url && url.StartsWith(DownloadPrefix))
                return new(Norm(v), url, a.GetProperty("size").GetInt64());
        return null;
    }

    /// <summary>Downloads next to the running exe, then swaps files. Windows allows renaming a running exe.</summary>
    public static async Task InstallAsync(UpdateInfo u, IProgress<double> progress)
    {
        var exe = Environment.ProcessPath!;
        var tmp = Path.Combine(Dir, "SkyDNS.new.exe");
        try
        {
            using var res = await Http.GetAsync(u.Url, HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();
            await using (var src = await res.Content.ReadAsStreamAsync())
            await using (var dst = File.Create(tmp))
            {
                var buf = new byte[1 << 16]; long done = 0; int n;
                while ((n = await src.ReadAsync(buf)) > 0)
                {
                    await dst.WriteAsync(buf.AsMemory(0, n));
                    progress.Report((double)(done += n) / u.Size);
                }
            }
            if (new FileInfo(tmp).Length != u.Size) throw new IOException(Loc.I["updBroken"]);
        }
        catch { try { File.Delete(tmp); } catch { } throw; }

        File.Delete(OldExe);
        File.Move(exe, OldExe);
        try { File.Move(tmp, exe); }
        catch { File.Move(OldExe, exe); throw; } // never leave the app without its exe
    }

    /// <summary>Remove the previous exe left behind by the last update.</summary>
    public static void CleanupOld() { try { File.Delete(OldExe); } catch { } }
}
