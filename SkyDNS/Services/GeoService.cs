using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkyDNS;

public record GeoInfo(string Ip, string Place, string Isp, double Lat, double Lon);

public static class GeoService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    /// <summary>Public IP + location via ipwho.is (free, HTTPS, no key). Null on any failure.</summary>
    public static async Task<GeoInfo?> LookupAsync()
    {
        try
        {
            using var doc = JsonDocument.Parse(await Http.GetStringAsync("https://ipwho.is/"));
            var r = doc.RootElement;
            if (!r.GetProperty("success").GetBoolean()) return null;
            string S(JsonElement e, string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
            var place = string.Join(", ", new[] { S(r, "city"), S(r, "country") }).Trim(' ', ',');
            var isp = r.TryGetProperty("connection", out var c) ? S(c, "isp") : "";
            return new GeoInfo(S(r, "ip"), place, isp, r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble());
        }
        catch { return null; }
    }
}
