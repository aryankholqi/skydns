using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SkyDNS;

/// <summary>
/// IPv4 input split into four octet boxes. Arrow keys / Backspace / "." move between octets,
/// a pasted IP fills all four, and a valid IP on the clipboard is offered in a popup on focus.
/// </summary>
public class IpBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(IpBox),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((IpBox)d).FromText()));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    readonly TextBox[] _parts = new TextBox[4];
    readonly Border _frame;
    readonly Popup _popup;
    readonly TextBlock _popupText;
    string _clip = "";
    bool _sync;

    public IpBox()
    {
        FlowDirection = FlowDirection.LeftToRight; // IPs read left-to-right even in Persian UI
        Height = 36;
        FontSize = 13;
        SetResourceReference(FontFamilyProperty, "MonoFont");

        var grid = new Grid { Margin = new Thickness(6, 0, 6, 0) };
        for (int i = 0; i < 4; i++)
        {
            if (i > 0)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var dot = new TextBlock { Text = ".", VerticalAlignment = VerticalAlignment.Center };
                dot.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
                Grid.SetColumn(dot, grid.ColumnDefinitions.Count - 1);
                grid.Children.Add(dot);
            }
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var tb = new TextBox
            {
                MaxLength = 3, BorderThickness = new Thickness(0), Background = Brushes.Transparent,
                TextAlignment = TextAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0), Tag = i,
            };
            tb.SetResourceReference(ForegroundProperty, "Ink");
            tb.SetResourceReference(TextBoxBase.CaretBrushProperty, "Sky");
            tb.SetResourceReference(TextBoxBase.SelectionBrushProperty, "Sky");
            tb.PreviewTextInput += Part_TextInput;
            tb.PreviewKeyDown += Part_KeyDown;
            tb.TextChanged += Part_TextChanged;
            tb.GotKeyboardFocus += (_, _) => tb.SelectAll();
            DataObject.AddPastingHandler(tb, Part_Paste);
            Grid.SetColumn(tb, grid.ColumnDefinitions.Count - 1);
            grid.Children.Add(tb);
            _parts[i] = tb;
        }

        _frame = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Child = grid };
        _frame.SetResourceReference(Border.BackgroundProperty, "Cloud");
        _frame.SetResourceReference(Border.BorderBrushProperty, "Line");
        _frame.MouseLeftButtonDown += (_, e) => { if (!IsKeyboardFocusWithin) { FirstEmpty().Focus(); e.Handled = true; } };

        _popupText = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var icon = new TextBlock { Text = "", FontSize = 12, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        icon.SetResourceReference(FontFamilyProperty, "IconFont");
        icon.SetResourceReference(TextBlock.ForegroundProperty, "Sky");
        var btn = new Button
        {
            Focusable = false, // keep keyboard focus in the octet boxes so the popup doesn't close itself
            Height = 30, Padding = new Thickness(10, 0, 10, 0), FontSize = 12,
            Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { icon, _popupText } },
        };
        btn.SetResourceReference(StyleProperty, "BtnSecondary");
        btn.Click += (_, _) => Insert(_clip);
        _popup = new Popup
        {
            PlacementTarget = this, Placement = PlacementMode.Top, VerticalOffset = -4,
            AllowsTransparency = true, StaysOpen = true, PopupAnimation = PopupAnimation.Fade,
            Child = new Border
            {
                Padding = new Thickness(3), CornerRadius = new CornerRadius(9), BorderThickness = new Thickness(1),
                Child = btn,
            },
        };
        ((Border)_popup.Child).SetResourceReference(Border.BackgroundProperty, "Cloud");
        ((Border)_popup.Child).SetResourceReference(Border.BorderBrushProperty, "Line");

        Content = _frame;
        IsKeyboardFocusWithinChanged += (_, _) => OnFocusWithin();
    }

    TextBox FirstEmpty() => _parts.FirstOrDefault(p => p.Text.Length == 0) ?? _parts[3];

    void OnFocusWithin()
    {
        _frame.SetResourceReference(Border.BorderBrushProperty, IsKeyboardFocusWithin ? "Sky" : "Line");
        if (!IsKeyboardFocusWithin) { _popup.IsOpen = false; return; }
        string clip = "";
        try { if (Clipboard.ContainsText()) clip = Clipboard.GetText().Trim(); } catch { } // clipboard can be locked by another app
        if (!DnsService.ValidIp(clip) || clip == Text) return;
        _clip = clip;
        _popupText.Text = Loc.I.F("pasteIp", clip);
        if (_popup.Child is FrameworkElement c) c.FlowDirection = Window.GetWindow(this)?.FlowDirection ?? FlowDirection.LeftToRight;
        _popup.IsOpen = true;
    }

    void Insert(string ip)
    {
        Text = ip;
        _popup.IsOpen = false;
        _parts[3].Focus();
        _parts[3].CaretIndex = _parts[3].Text.Length;
    }

    void Move(int from, int to, bool caretAtEnd)
    {
        if (to < 0 || to > 3) return;
        var t = _parts[to];
        t.Focus();
        t.Select(caretAtEnd ? t.Text.Length : 0, 0);
    }

    void Part_TextInput(object s, TextCompositionEventArgs e)
    {
        var tb = (TextBox)s; int i = (int)tb.Tag;
        e.Handled = true;
        if (e.Text is "." or "/" or ",")
        {
            if (tb.Text.Length > 0) { Move(i, i + 1, false); _parts[Math.Min(i + 1, 3)].SelectAll(); }
            return;
        }
        if (!e.Text.All(char.IsAsciiDigit)) return;
        var next = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text);
        if (next.Length > 3 || int.Parse(next) > 255)
        {
            // octet is full: overflow the typed digit into the next box
            if (i < 3 && tb.SelectionLength == 0 && tb.CaretIndex == tb.Text.Length) { _parts[i + 1].Focus(); _parts[i + 1].SelectedText = e.Text; _parts[i + 1].CaretIndex = _parts[i + 1].Text.Length; }
            return;
        }
        e.Handled = false;
    }

    void Part_KeyDown(object s, KeyEventArgs e)
    {
        var tb = (TextBox)s; int i = (int)tb.Tag;
        bool noSel = tb.SelectionLength == 0;
        switch (e.Key)
        {
            case Key.Escape when _popup.IsOpen: _popup.IsOpen = false; e.Handled = true; break;
            case Key.Space: e.Handled = true; break;
            case Key.Left when noSel && tb.CaretIndex == 0 && i > 0:
            case Key.Back when noSel && tb.CaretIndex == 0 && i > 0:
                Move(i, i - 1, true); e.Handled = true; break;
            case Key.Right when noSel && tb.CaretIndex == tb.Text.Length && i < 3:
                Move(i, i + 1, false); e.Handled = true; break;
            case Key.Home when Keyboard.Modifiers == ModifierKeys.None: Move(i, 0, false); e.Handled = true; break;
            case Key.End when Keyboard.Modifiers == ModifierKeys.None: Move(i, 3, true); e.Handled = true; break;
        }
    }

    void Part_TextChanged(object s, TextChangedEventArgs e)
    {
        var tb = (TextBox)s; int i = (int)tb.Tag;
        if (_sync) return;
        ToText();
        // auto-advance once no further digit could fit (e.g. "192" or "26")
        if (i < 3 && tb.IsKeyboardFocused && tb.CaretIndex == tb.Text.Length && tb.Text.Length > 0
            && (tb.Text.Length == 3 || int.Parse(tb.Text) * 10 > 255))
        { _parts[i + 1].Focus(); _parts[i + 1].SelectAll(); }
    }

    void Part_Paste(object s, DataObjectPastingEventArgs e)
    {
        e.CancelCommand();
        var text = (e.DataObject.GetData(DataFormats.UnicodeText) as string ?? "").Trim();
        if (text.Contains('.')) { if (DnsService.ValidIp(text)) Insert(text); return; }
        var tb = (TextBox)s;
        if (text.Length > 0 && text.All(char.IsAsciiDigit))
        {
            var next = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, text);
            if (next.Length <= 3 && int.Parse(next) <= 255) tb.SelectedText = text;
        }
    }

    void ToText()
    {
        _sync = true;
        Text = _parts.All(p => p.Text.Length == 0) ? "" : string.Join(".", _parts.Select(p => p.Text));
        _sync = false;
        if (_popup.IsOpen && Text == _clip) _popup.IsOpen = false;
    }

    void FromText()
    {
        if (_sync) return;
        _sync = true;
        var seg = (Text ?? "").Split('.');
        for (int i = 0; i < 4; i++)
        {
            var v = i < seg.Length ? new string(seg[i].Where(char.IsAsciiDigit).Take(3).ToArray()) : "";
            if (_parts[i].Text != v) _parts[i].Text = v;
        }
        _sync = false;
    }
}
