using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace SkyDNS;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void On([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; On(name); return true;
    }
}

public static class Ui
{
    public static SolidColorBrush Brush(string hex, byte? alpha = null)
    {
        Color c;
        try { c = (Color)ColorConverter.ConvertFromString(hex); } catch { c = Color.FromRgb(0, 0x99, 0xFF); }
        if (alpha is byte a) c.A = a;
        var b = new SolidColorBrush(c); b.Freeze(); return b;
    }
}

public static class Icons
{
    public static readonly Dictionary<string, string> Map = new()
    {
        ["globe"] = "\uE774", ["shield"] = "\uEA18", ["lightning"] = "\uE945", ["game"] = "\uE7FC",
        ["home"] = "\uE80F", ["briefcase"] = "\uE821", ["lock"] = "\uE72E", ["cloud"] = "\uE753",
    };
    public static string Glyph(string key) => Map.TryGetValue(key, out var g) ? g : Map["globe"];
}

public class DnsEntry : ObservableObject
{
    static readonly Brush GoodB = Ui.Brush("#22A55B"), WarnB = Ui.Brush("#E59A0B"), BadB = Ui.Brush("#DC4A3D"), IdleB = Ui.Brush("#C3CBD5");

    string _name = "", _primary = "", _secondary = "", _color = "#0099FF", _icon = "globe";
    int? _ping; bool _pinging, _active;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get => _name; set => Set(ref _name, value); }
    public string Primary { get => _primary; set => Set(ref _primary, value); }
    public string Secondary { get => _secondary; set { if (Set(ref _secondary, value)) On(nameof(SecondaryText)); } }
    public string Color { get => _color; set { if (Set(ref _color, value)) { On(nameof(ColorBrush)); On(nameof(SoftBrush)); } } }
    public string Icon { get => _icon; set { if (Set(ref _icon, value)) On(nameof(IconGlyph)); } }

    [JsonIgnore] public int? Ping { get => _ping; set { _ping = value; On(string.Empty); } }
    [JsonIgnore] public bool IsPinging { get => _pinging; set { _pinging = value; On(string.Empty); } }
    [JsonIgnore] public bool IsActive { get => _active; set { _active = value; On(string.Empty); } }

    [JsonIgnore] public Brush ColorBrush => Ui.Brush(Color);
    [JsonIgnore] public Brush SoftBrush => Ui.Brush(Color, 0x22);
    [JsonIgnore] public string IconGlyph => Icons.Glyph(Icon);
    [JsonIgnore] public string SecondaryText => string.IsNullOrWhiteSpace(Secondary) ? "—" : Secondary;
    [JsonIgnore] public string PingText => IsPinging ? "…" : Ping is int p ? $"{p} ms" : "—";
    [JsonIgnore] public string PingNum => IsPinging ? "…" : Ping?.ToString() ?? "—";
    [JsonIgnore] public double PingValue => Math.Min(200, Ping ?? 0);
    [JsonIgnore] public Brush PingBrush => IsPinging || Ping == null ? IdleB : Ping < 60 ? GoodB : Ping < 120 ? WarnB : BadB;
    [JsonIgnore] public string Quality => IsPinging || Ping == null ? "" : Loc.I[Ping < 60 ? "good" : Ping < 120 ? "ok" : "slow"];

    public void Touch() => On(string.Empty);
}

public class AppSettings
{
    public string Language { get; set; } = "fa";
    public string Adapter { get; set; } = "auto";
    public bool Startup { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool FlushAfterSwitch { get; set; } = true;
    public bool RestoreOnExit { get; set; }
    public bool PingOnLaunch { get; set; } = true;
    public bool WidgetVisible { get; set; }
    public double? WidgetLeft { get; set; }
    public double? WidgetTop { get; set; }
    public string? ActiveId { get; set; }
    public List<DnsEntry> Servers { get; set; } = new();

    public static AppSettings CreateDefault() => new()
    {
        Servers = new()
        {
            new() { Name = "Shecan", Primary = "178.22.122.100", Secondary = "185.51.200.2", Color = "#0099FF", Icon = "shield" },
            new() { Name = "Electro", Primary = "78.157.42.100", Secondary = "78.157.42.101", Color = "#F79009", Icon = "lightning" },
            new() { Name = "403 Online", Primary = "10.202.10.202", Secondary = "10.202.10.102", Color = "#7A5AF8", Icon = "game" },
            new() { Name = "Cloudflare", Primary = "1.1.1.1", Secondary = "1.0.0.1", Color = "#EE6B1F", Icon = "cloud" },
            new() { Name = "Google", Primary = "8.8.8.8", Secondary = "8.8.4.4", Color = "#12B76A", Icon = "globe" },
            new() { Name = "Quad9", Primary = "9.9.9.9", Secondary = "149.112.112.112", Color = "#EE46BC", Icon = "lock" },
        }
    };
}

public class AdapterOption
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Desc { get; init; } = "";
    public string Glyph { get; init; } = "";
}
