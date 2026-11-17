using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SkyDNS;

public partial class EditWindow : Window
{
    public class Opt
    {
        public string Value { get; init; } = "";
        public Brush? Brush { get; init; }
        public string Glyph { get; init; } = "";
        public bool IsChecked { get; init; }
    }

    static readonly string[] Colors = { "#0099FF", "#7A5AF8", "#12B76A", "#F79009", "#EE46BC", "#0BA5A0", "#EE6B1F", "#5E6E82" };

    readonly DnsEntry? _existing;
    string _color, _icon;

    public EditWindow(DnsEntry? existing)
    {
        InitializeComponent();
        DataContext = MainViewModel.I;
        _existing = existing;
        _color = existing?.Color ?? Colors[0];
        _icon = existing?.Icon ?? "globe";

        TitleText.Text = Loc.I[existing == null ? "addTitle" : "editTitle"];
        NameBox.Text = existing?.Name ?? "";
        PrimaryBox.Text = existing?.Primary ?? "";
        SecondaryBox.Text = existing?.Secondary ?? "";

        var colors = Colors.Contains(_color) ? Colors : Colors.Append(_color).ToArray();
        ColorList.ItemsSource = colors.Select(c => new Opt { Value = c, Brush = Ui.Brush(c), IsChecked = c == _color }).ToList();
        IconList.ItemsSource = Icons.Map.Select(kv => new Opt { Value = kv.Key, Glyph = kv.Value, IsChecked = kv.Key == _icon }).ToList();

        UpdatePreview();
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    void UpdatePreview()
    {
        Preview.Background = Ui.Brush(_color, 0x22);
        PreviewGlyph.Foreground = Ui.Brush(_color);
        PreviewGlyph.Text = Icons.Glyph(_icon);
    }

    void Color_Click(object s, RoutedEventArgs e) { _color = (string)((FrameworkElement)s).Tag; UpdatePreview(); }
    void Icon_Click(object s, RoutedEventArgs e) { _icon = (string)((FrameworkElement)s).Tag; UpdatePreview(); }
    void Drag(object s, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
    void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

    static bool ValidIp(string s) => DnsService.ValidIp(s);

    void ShowError(string key)
    {
        ErrorText.Text = Loc.I[key];
        ErrorText.Visibility = Visibility.Visible;
    }

    void Save_Click(object s, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var p = PrimaryBox.Text.Trim();
        var sec = SecondaryBox.Text.Trim();
        if (name.Length == 0 || p.Length == 0) { ShowError("errName"); return; }
        if (!ValidIp(p) || (sec.Length > 0 && !ValidIp(sec))) { ShowError("errIp"); return; }

        var vm = MainViewModel.I;
        if (_existing == null)
        {
            vm.Add(new DnsEntry { Name = name, Primary = p, Secondary = sec, Color = _color, Icon = _icon });
        }
        else
        {
            bool changed = _existing.Primary != p || _existing.Secondary != sec;
            _existing.Name = name; _existing.Primary = p; _existing.Secondary = sec;
            _existing.Color = _color; _existing.Icon = _icon;
            vm.Updated(_existing, changed);
        }
        DialogResult = true;
    }
}
