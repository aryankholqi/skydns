using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using WF = System.Windows.Forms;

namespace SkyDNS;

public sealed class TrayService : IDisposable
{
    readonly WF.NotifyIcon _icon;
    readonly Icon _on, _off;
    static MainViewModel VM => MainViewModel.I;

    public TrayService()
    {
        _on = MakeIcon(Color.FromArgb(0, 153, 255));
        _off = MakeIcon(Color.FromArgb(138, 150, 166));
        _icon = new WF.NotifyIcon { Visible = true, ContextMenuStrip = new WF.ContextMenuStrip() };
        _icon.ContextMenuStrip.Opening += (_, e) => { Build(); e.Cancel = false; };
        _icon.MouseClick += (_, e) => { if (e.Button == WF.MouseButtons.Left) App.ShowMain(); };
        Build();
        Update();
        VM.PropertyChanged += (_, _) => Update();
    }

    void Update()
    {
        var a = VM.Active;
        _icon.Icon = a != null ? _on : _off;
        var text = $"{Loc.I["appName"]} — {a?.Name ?? Loc.I["autoDns"]}";
        _icon.Text = text.Length > 63 ? text[..63] : text;
    }

    void Build()
    {
        var m = _icon.ContextMenuStrip!;
        m.Items.Clear();
        m.RightToLeft = Loc.I.IsRtl ? WF.RightToLeft.Yes : WF.RightToLeft.No;
        m.ShowCheckMargin = true;
        m.ShowImageMargin = false;

        m.Items.Add(new WF.ToolStripMenuItem(VM.TrayStatus) { Enabled = false });
        m.Items.Add(new WF.ToolStripSeparator());
        foreach (var s in VM.Servers.ToList())
        {
            var item = new WF.ToolStripMenuItem(s.Name) { Checked = s.IsActive, ShortcutKeyDisplayString = s.PingText };
            item.Click += async (_, _) => await VM.ApplyAsync(s);
            m.Items.Add(item);
        }
        m.Items.Add(new WF.ToolStripSeparator());
        Add(m, Loc.I["disconnect"], async () => await VM.DisconnectAsync(), VM.AnyActive);
        Add(m, VM.TestAllLabel, async () => await VM.TestAllAsync(), !VM.IsTesting);
        m.Items.Add(new WF.ToolStripSeparator());
        Add(m, Loc.I["open"], App.ShowMain);
        Add(m, Loc.I["widget"], () => App.ToggleWidget());
        Add(m, Loc.I["exit"], App.ExitApp);
    }

    static void Add(WF.ContextMenuStrip m, string text, Action action, bool enabled = true)
    {
        var i = new WF.ToolStripMenuItem(text) { Enabled = enabled };
        i.Click += (_, _) => action();
        m.Items.Add(i);
    }

    static Icon MakeIcon(Color bg)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            const int x = 1, y = 1, w = 30, h = 30, r = 16;
            path.AddArc(x, y, r, r, 180, 90);
            path.AddArc(x + w - r, y, r, r, 270, 90);
            path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            path.AddArc(x, y + h - r, r, r, 90, 90);
            path.CloseFigure();
            using var b = new SolidBrush(bg);
            g.FillPath(b, path);
            using var white = new SolidBrush(Color.FromArgb(253, 253, 254));
            g.FillEllipse(white, 6, 14, 10, 10);
            g.FillEllipse(white, 11, 8, 12, 12);
            g.FillEllipse(white, 17, 13, 10, 10);
            g.FillRectangle(white, 11, 18, 11, 6);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
