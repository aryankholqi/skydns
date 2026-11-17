using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace SkyDNS;

public partial class MainWindow : Window
{
    static MainViewModel VM => MainViewModel.I;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = VM;
        StateChanged += (_, _) =>
        {
            bool max = WindowState == WindowState.Maximized;
            Root.Margin = max ? new Thickness(7) : new Thickness(0);
            MaxBtn.Content = max ? "\uE923" : "\uE922";
        };
    }

    void Min_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    void Max_Click(object s, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    void Close_Click(object s, RoutedEventArgs e) => Close();
    void Widget_Click(object s, RoutedEventArgs e) => App.ToggleWidget();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.IsExiting)
        {
            e.Cancel = true;
            if (VM.Settings.MinimizeToTray) Hide();
            else App.ExitApp();
        }
        base.OnClosing(e);
    }

    void Item_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DnsEntry d }) _ = VM.ApplyAsync(d);
    }

    void Item_KeyDown(object sender, KeyEventArgs e)
    {
        if ((e.Key == Key.Enter || e.Key == Key.Space) && sender is FrameworkElement { DataContext: DnsEntry d })
        {
            _ = VM.ApplyAsync(d);
            e.Handled = true;
        }
    }

    void Toggle_Click(object s, RoutedEventArgs e) => _ = VM.ToggleAsync();
    void TestAll_Click(object s, RoutedEventArgs e) => _ = VM.TestAllAsync();
    void Test_Click(object s, RoutedEventArgs e) => _ = VM.TestOneAsync(VM.Selected);

    void Add_Click(object s, RoutedEventArgs e) => new EditWindow(null) { Owner = this }.ShowDialog();
    void Edit_Click(object s, RoutedEventArgs e)
    {
        if (VM.Selected != null) new EditWindow(VM.Selected) { Owner = this }.ShowDialog();
    }
    void Settings_Click(object s, RoutedEventArgs e) => new SettingsWindow { Owner = this }.ShowDialog();

    void Delete_Click(object s, RoutedEventArgs e)
    {
        if (VM.Selected is not { } d) return;
        var opts = Loc.I.IsRtl ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : MessageBoxOptions.None;
        if (MessageBox.Show(Loc.I.F("confirmDel", d.Name), Loc.I["appName"], MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, opts) == MessageBoxResult.Yes)
            VM.Delete(d);
    }

    void CopyP_Click(object s, RoutedEventArgs e) => Copy(CopyP, VM.Selected?.Primary);
    void CopyS_Click(object s, RoutedEventArgs e) => Copy(CopyS, VM.Selected?.Secondary);

    static async void Copy(Button b, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        try { Clipboard.SetText(text); } catch { return; }
        b.Content = "\uE73E";
        await Task.Delay(1200);
        b.Content = "\uE8C8";
    }

    void Adapter_Click(object sender, RoutedEventArgs e)
    {
        VM.RefreshAdapters();
        var menu = new ContextMenu
        {
            PlacementTarget = (UIElement)sender,
            Placement = PlacementMode.Bottom,
            FlowDirection = VM.Flow,
            FontFamily = FontFamily,
        };
        foreach (var a in VM.AdapterOptions)
        {
            var opt = a;
            var mi = new MenuItem { Header = a.Label, IsChecked = a.Key == VM.Settings.Adapter, ToolTip = a.Desc };
            mi.Click += (_, _) => VM.SelectedAdapter = opt;
            menu.Items.Add(mi);
        }
        menu.IsOpen = true;
    }
}

/// <summary>Maps the window flow direction to the "start" horizontal alignment for LTR-forced content.</summary>
public class FlowAlign : IValueConverter
{
    public static FlowAlign I { get; } = new();
    public object Convert(object value, Type t, object p, CultureInfo c) =>
        value is FlowDirection.RightToLeft ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
}
