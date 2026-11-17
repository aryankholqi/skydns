using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkyDNS;

public static class PingService
{
    /// <summary>ICMP round-trip (same as cmd ping); falls back to a DNS query if ICMP is blocked.</summary>
    public static async Task<int?> MeasureAsync(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return null;
        // ICMP first: many ISPs hijack UDP 53 and answer locally, so DNS timing reports the ISP, not the server.
        // shortcut: the fallback can still show a hijacked time for ICMP-blocking servers; detect hijack if that matters.
        return await IcmpAsync(addr) ?? await DnsQueryAsync(addr);
    }

    static async Task<int?> DnsQueryAsync(IPAddress addr)
    {
        try
        {
            using var udp = new UdpClient(addr.AddressFamily);
            udp.Connect(addr, 53);
            var q = BuildQuery("www.google.com");
            var sw = Stopwatch.StartNew();
            await udp.SendAsync(q, q.Length);
            using var cts = new CancellationTokenSource(2000);
            await udp.ReceiveAsync(cts.Token);
            return (int)Math.Max(1, sw.ElapsedMilliseconds);
        }
        catch { return null; }
    }

    static async Task<int?> IcmpAsync(IPAddress addr)
    {
        try
        {
            using var p = new Ping();
            var r = await p.SendPingAsync(addr, 1500);
            return r.Status == IPStatus.Success ? (int)Math.Max(1, r.RoundtripTime) : null;
        }
        catch { return null; }
    }

    static byte[] BuildQuery(string host)
    {
        var id = (ushort)Random.Shared.Next(ushort.MaxValue);
        var b = new List<byte> { (byte)(id >> 8), (byte)id, 0x01, 0x00, 0, 1, 0, 0, 0, 0, 0, 0 };
        foreach (var part in host.Split('.'))
        {
            b.Add((byte)part.Length);
            b.AddRange(Encoding.ASCII.GetBytes(part));
        }
        b.AddRange(new byte[] { 0, 0, 1, 0, 1 }); // end name, QTYPE=A, QCLASS=IN
        return b.ToArray();
    }
}
