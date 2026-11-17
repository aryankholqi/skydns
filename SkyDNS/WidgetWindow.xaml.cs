using System.Windows;
using System.Windows.Input;

namespace SkyDNS;

public partial class WidgetWindow : Window
{
    static MainViewModel VM => MainViewModel.I;

    public WidgetWindow()
    {
        InitializeComponent();
        DataContext = VM;
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = VM.Settings.WidgetLeft ?? area.Right - ActualWidth;
            Top = VM.Settings.WidgetTop ?? area.Bottom - ActualHeight;
            // keep on-screen if the saved position is off the current display
            if (Left < area.Left - 20 || Left > area.Right - 100) Left = area.Right - ActualWidth;
            if (Top < area.Top - 20 || Top > area.Bottom - 60) Top = area.Bottom - ActualHeight;
        };
    }

    void Drag(object s, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        DragMove();
        VM.Settings.WidgetLeft = Left;
        VM.Settings.WidgetTop = Top;
        VM.Save();
    }

    void Tile_Click(object s, RoutedEventArgs e)
    {
        if (s is FrameworkElement { DataContext: DnsEntry d }) _ = VM.ApplyAsync(d);
    }

    void Toggle_Click(object s, RoutedEventArgs e) => _ = VM.WidgetToggleAsync();
    void Open_Click(object s, RoutedEventArgs e) => App.ShowMain();
    void Close_Click(object s, RoutedEventArgs e) => App.ToggleWidget(false);
}
