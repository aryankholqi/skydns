using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SkyDNS;

public static class DnsService
{
    public const string AllKey = "*all*";
    public const string AutoKey = "auto";

    static readonly string[] Skip = { "Virtual", "Hyper-V", "VMware", "VirtualBox", "Loopback", "Bluetooth", "TAP-", "Wintun", "WAN Miniport", "Npcap" };

    static DnsService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Strict dotted IPv4 — these strings go straight into netsh arguments.</summary>
    public static bool ValidIp(string s) =>
        s.Count(c => c == '.') == 3 && IPAddress.TryParse(s, out var a) && a.AddressFamily == AddressFamily.InterNetwork;

    [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
    static extern bool DnsFlushResolverCache();

    public static List<NetworkInterface> Physical() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => (n.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                  || n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                  || n.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                 && !Skip.Any(k => n.Description.Contains(k, StringComparison.OrdinalIgnoreCase)))
        .OrderByDescending(n => n.OperationalStatus == OperationalStatus.Up)
        .ToList();

    /// <summary>Adapter names a setting key resolves to.</summary>
    public static List<string> Resolve(string key)
    {
        var phys = Physical();
        var up = phys.Where(n => n.OperationalStatus == OperationalStatus.Up).ToList();
        if (key == AllKey) return up.Select(n => n.Name).ToList();
        if (key != AutoKey) return new List<string> { key };
        var best = up.FirstOrDefault(n => n.GetIPProperties().GatewayAddresses
            .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)));
        return (best ?? up.FirstOrDefault()) is { } b ? new List<string> { b.Name } : new List<string>();
    }

    public static IEnumerable<string> GetCurrent(string adapterName)
    {
        var n = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == adapterName);
        if (n == null) return Array.Empty<string>();
        return n.GetIPProperties().DnsAddresses
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.ToString()).ToList();
    }

    public static async Task SetAsync(string primary, string? secondary, IEnumerable<string> adapters, bool flush)
    {
        var list = adapters.ToList();
        if (list.Count == 0) throw new InvalidOperationException(Loc.I["errNoAdapter"]);
        var cmds = new List<string>();
        foreach (var a in list)
        {
            cmds.Add($"interface ipv4 set dnsservers name=\"{a}\" source=static address={primary} register=primary validate=no");
            if (!string.IsNullOrWhiteSpace(secondary))
                cmds.Add($"interface ipv4 add dnsservers name=\"{a}\" address={secondary} index=2 validate=no");
        }
        await Netsh(cmds);
        if (flush) DnsFlushResolverCache();
    }

    public static async Task ResetAsync(IEnumerable<string> adapters, bool flush)
    {
        var list = adapters.ToList();
        if (list.Count == 0) throw new InvalidOperationException(Loc.I["errNoAdapter"]);
        await Netsh(list.Select(a => $"interface ipv4 set dnsservers name=\"{a}\" source=dhcp").ToList());
        if (flush) DnsFlushResolverCache();
    }

    static bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    static async Task Netsh(List<string> cmds)
    {
        if (IsAdmin) { foreach (var c in cmds) await Netsh(c); return; }
        // App runs unelevated: chain all commands in one elevated cmd so the user sees a single UAC prompt.
        // shortcut: elevated output can't be redirected, so errors show only the exit code.
        var psi = new ProcessStartInfo("cmd.exe", "/c " + string.Join(" && ", cmds.Select(c => "netsh " + c)))
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        Process p;
        try { p = Process.Start(psi) ?? throw new InvalidOperationException("netsh failed to start"); }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // user clicked No on UAC
        { throw new InvalidOperationException("Administrator permission was denied."); }
        using (p)
        {
            await p.WaitForExitAsync();
            if (p.ExitCode != 0) throw new InvalidOperationException($"netsh exit code {p.ExitCode}");
        }
    }

    static async Task Netsh(string args)
    {
        var enc = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        var psi = new ProcessStartInfo("netsh", args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = enc,
            StandardErrorEncoding = enc,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("netsh failed to start");
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        var output = ((await outTask) + (await errTask)).Trim();
        if (p.ExitCode != 0) throw new InvalidOperationException(string.IsNullOrEmpty(output) ? $"netsh exit code {p.ExitCode}" : output);
    }
}
