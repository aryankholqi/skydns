using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SkyDNS;

public class MainViewModel : ObservableObject
{
    public static MainViewModel I { get; } = new();

    public AppSettings Settings { get; }
    public ObservableCollection<DnsEntry> Servers { get; }
    public ICollectionView ServersView { get; }
    public event Action<string>? Error;

    static readonly Brush Sky = Ui.Brush("#0099FF"), Cloud = Ui.Brush("#FDFDFE"), Pane = Ui.Brush("#F5F7FA"),
        Line = Ui.Brush("#E3E8EF"), SoftLine = Ui.Brush("#BCDDFB"), Grey = Ui.Brush("#A7B1BE"), Muted = Ui.Brush("#7D8A9A"),
        Ring = Ui.Brush("#D3DBE5"), Good = Ui.Brush("#22A55B"), Warn = Ui.Brush("#E59A0B"),
        Halo = Ui.Brush("#DCEEFF"), HaloSoft = Ui.Brush("#EEF6FF"), Clear = Brushes.Transparent;
    static readonly Brush HeroGrad = MakeGrad();

    static Brush MakeGrad()
    {
        var g = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#E9F5FF"), (Color)ColorConverter.ConvertFromString("#FDFDFE"), new Point(0, 0), new Point(1, 0.6));
        g.Freeze(); return g;
    }

    MainViewModel()
    {
        Settings = Storage.Load();
        Loc.I.Lang = Settings.Language;
        Servers = new ObservableCollection<DnsEntry>(Settings.Servers);
        ServersView = new ListCollectionView(Servers)
        {
            Filter = o => o is DnsEntry d && (_search.Trim().Length == 0
                || d.Name.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase)
                || d.Primary.Contains(_search.Trim()))
        };
        _selected = Servers.FirstOrDefault(s => s.Id == Settings.ActiveId) ?? Servers.FirstOrDefault();
        RefreshAdapters();
    }

    static string T(string k) => Loc.I[k];

    // ---------- state ----------
    string _search = "";
    public string Search { get => _search; set { if (Set(ref _search, value)) { ServersView.Refresh(); On(nameof(SearchEmpty)); } } }
    public bool SearchEmpty => _search.Length == 0;

    DnsEntry? _selected;
    public DnsEntry? Selected { get => _selected; set { if (Set(ref _selected, value)) On(string.Empty); } }

    string? _activeId;
    public DnsEntry? Active => _activeId == null ? null : Servers.FirstOrDefault(s => s.Id == _activeId);

    bool _applying, _testing;
    public bool IsApplying { get => _applying; private set { _applying = value; On(string.Empty); } }
    public bool IsTesting { get => _testing; private set { _testing = value; On(string.Empty); } }
    public bool AnyActive => Active != null;
    public bool IsConnected => !_applying && Active != null && Active == _selected;
    public bool HasSelection => _selected != null;
    public FlowDirection Flow => Loc.I.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // ---------- derived UI ----------
    string _adapterSummary = "—";
    public string StatusTitle => _applying ? T("applying") : IsConnected ? T("connected") : AnyActive ? T("connectedOther") : T("notConnected");
    public string StatusDesc => _applying ? T("applyDesc") : IsConnected ? Loc.I.F("connDesc", _adapterSummary)
        : AnyActive ? Loc.I.F("otherDesc", Active!.Name) : T("offDesc");
    public Brush StatusDot => _applying ? Warn : AnyActive ? Good : Grey;
    public Brush HeroBg => IsConnected ? HeroGrad : Pane;
    public Brush HeroBorder => IsConnected ? SoftLine : Line;
    public Brush CircleBg => IsConnected ? Sky : Cloud;
    public Brush CircleBorder => IsConnected || _applying ? Sky : Ring;
    public Brush CircleIcon => IsConnected ? Cloud : _applying ? Sky : Muted;
    public Brush HaloBrush => IsConnected ? Halo : _applying ? HaloSoft : Clear;
    public string CtaLabel => IsConnected ? T("disconnect") : T("connect");
    public string CtaGlyph => IsConnected ? "\uE711" : "\uE7E8";
    public string TestLabel => _testing ? T("testing") : T("test");
    public string TestAllLabel => _testing ? T("testing") : T("testAll");
    public string TrayStatus => _applying ? T("applying") : Active is { } a ? $"{T("connected")} · {a.Name}" : T("autoDns");
    public string WidgetName => Active?.Name ?? T("autoDns");
    public string WidgetSub => Active is { } a ? $"{a.Primary}  ·  {a.PingText}" : "DHCP";
    public Brush WidgetBg => AnyActive ? HeroGrad : Pane;
    public Brush WidgetBorder => AnyActive ? SoftLine : Line;

    // ---------- adapters ----------
    public List<AdapterOption> AdapterOptions { get; private set; } = new();
    public AdapterOption? SelectedAdapter
    {
        get => AdapterOptions.FirstOrDefault(a => a.Key == Settings.Adapter) ?? AdapterOptions.FirstOrDefault();
        set
        {
            if (value == null || value.Key == Settings.Adapter) return;
            Settings.Adapter = value.Key;
            Save();
            RefreshAdapters();
            if (Active is { } a) { _activeId = null; _ = ApplyAsync(a); } // re-apply on the new adapter
        }
    }
    public string AdapterLabel => SelectedAdapter?.Label ?? "—";
    public string AdapterGlyph => SelectedAdapter?.Glyph ?? "\uE774";

    public void RefreshAdapters()
    {
        var phys = DnsService.Physical();
        var auto = DnsService.Resolve(DnsService.AutoKey);
        var list = new List<AdapterOption>
        {
            new() { Key = DnsService.AutoKey, Label = T("autoAdapter"), Desc = Loc.I.F("autoAdapterD", auto.FirstOrDefault() ?? "—"), Glyph = "\uE774" }
        };
        list.AddRange(phys.Select(n => new AdapterOption
        {
            Key = n.Name,
            Label = n.Name,
            Desc = $"{n.Description} · {(n.OperationalStatus == OperationalStatus.Up ? T("up") : T("down"))}",
            Glyph = n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "\uE701" : "\uE839",
        }));
        if (Settings.Adapter != DnsService.AutoKey && Settings.Adapter != DnsService.AllKey && phys.All(n => n.Name != Settings.Adapter))
            list.Add(new() { Key = Settings.Adapter, Label = Settings.Adapter, Desc = T("down"), Glyph = "\uE839" });
        list.Add(new() { Key = DnsService.AllKey, Label = T("all"), Desc = T("allD"), Glyph = "\uE8A9" });
        AdapterOptions = list;
        var targets = DnsService.Resolve(Settings.Adapter);
        _adapterSummary = targets.Count > 0 ? string.Join(", ", targets) : "—";
        On(string.Empty);
    }

    // ---------- actions ----------
    public async Task ApplyAsync(DnsEntry d)
    {
        if (_applying) return;
        Selected = d;
        if (_activeId == d.Id) return;
        IsApplying = true;
        try
        {
            var targets = DnsService.Resolve(Settings.Adapter);
            await DnsService.SetAsync(d.Primary, d.Secondary, targets, Settings.FlushAfterSwitch);
            _adapterSummary = string.Join(", ", targets);
            SetActive(d.Id);
        }
        catch (Exception ex) { Error?.Invoke(ex.Message); }
        finally { IsApplying = false; }
    }

    public async Task DisconnectAsync()
    {
        if (_applying) return;
        IsApplying = true;
        try
        {
            await DnsService.ResetAsync(DnsService.Resolve(Settings.Adapter), Settings.FlushAfterSwitch);
            SetActive(null);
        }
        catch (Exception ex) { Error?.Invoke(ex.Message); }
        finally { IsApplying = false; }
    }

    public Task ToggleAsync() => IsConnected ? DisconnectAsync() : _selected != null ? ApplyAsync(_selected) : Task.CompletedTask;

    public Task WidgetToggleAsync() => AnyActive ? DisconnectAsync()
        : (_selected ?? Servers.FirstOrDefault()) is { } d ? ApplyAsync(d) : Task.CompletedTask;

    void SetActive(string? id)
    {
        _activeId = id;
        Settings.ActiveId = id;
        foreach (var s in Servers) s.IsActive = s.Id == id;
        Save();
        On(string.Empty);
    }

    /// <summary>Match Windows' current DNS against saved entries.</summary>
    public void DetectActive()
    {
        try
        {
            var current = DnsService.Resolve(Settings.Adapter).SelectMany(DnsService.GetCurrent).FirstOrDefault();
            var match = current == null ? null : Servers.FirstOrDefault(s => s.Primary == current);
            _activeId = match?.Id;
            foreach (var s in Servers) s.IsActive = s.Id == _activeId;
            if (match != null) _selected = match;
            On(string.Empty);
        }
        catch { }
    }

    public async Task TestAllAsync()
    {
        if (_testing) return;
        IsTesting = true;
        var list = Servers.ToList();
        foreach (var s in list) s.IsPinging = true;
        await Task.WhenAll(list.Select(async s =>
        {
            var p = await PingService.MeasureAsync(s.Primary);
            s.Ping = p; s.IsPinging = false;
        }));
        IsTesting = false;
    }

    public async Task TestOneAsync(DnsEntry? d)
    {
        if (d == null) return;
        d.IsPinging = true; On(string.Empty);
        var p = await PingService.MeasureAsync(d.Primary);
        d.Ping = p; d.IsPinging = false; On(string.Empty);
    }

    public void Add(DnsEntry d)
    {
        Servers.Add(d);
        Selected = d;
        Save();
        _ = TestOneAsync(d);
    }

    public void Updated(DnsEntry d, bool addressesChanged)
    {
        Save();
        d.Touch();
        On(string.Empty);
        if (addressesChanged)
        {
            _ = TestOneAsync(d);
            if (d.Id == _activeId) { _activeId = null; _ = ApplyAsync(d); }
        }
    }

    public void Delete(DnsEntry d)
    {
        var idx = Servers.IndexOf(d);
        Servers.Remove(d);
        if (_activeId == d.Id) { _activeId = null; Settings.ActiveId = null; }
        Selected = Servers.ElementAtOrDefault(Math.Max(0, idx - 1));
        Save();
    }

    public void Save()
    {
        Settings.Servers = Servers.ToList();
        Storage.Save(Settings);
    }

    // ---------- settings ----------
    public bool StartupEnabled
    {
        get => Settings.Startup;
        set
        {
            try { StartupService.Set(value); Settings.Startup = value; Save(); }
            catch (Exception ex) { Error?.Invoke(ex.Message); }
            On();
        }
    }
    public bool MinimizeToTray { get => Settings.MinimizeToTray; set { Settings.MinimizeToTray = value; Save(); On(); } }
    public bool FlushAfterSwitch { get => Settings.FlushAfterSwitch; set { Settings.FlushAfterSwitch = value; Save(); On(); } }
    public bool RestoreOnExit { get => Settings.RestoreOnExit; set { Settings.RestoreOnExit = value; Save(); On(); } }
    public bool PingOnLaunch { get => Settings.PingOnLaunch; set { Settings.PingOnLaunch = value; Save(); On(); } }

    public bool IsFa { get => Loc.I.Lang == "fa"; set { if (value) SetLanguage("fa"); } }
    public bool IsEn { get => Loc.I.Lang == "en"; set { if (value) SetLanguage("en"); } }

    public void SetLanguage(string lang)
    {
        if (Settings.Language == lang) return;
        Settings.Language = lang;
        Loc.I.Lang = lang;
        Save();
        foreach (var s in Servers) s.Touch();
        RefreshAdapters();
    }
}
