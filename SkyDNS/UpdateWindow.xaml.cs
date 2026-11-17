using System;
using System.Windows;
using System.Windows.Input;

namespace SkyDNS;

/// <summary>Offers a new release; downloads, swaps the exe and restarts on confirm.</summary>
public partial class UpdateWindow : Window
{
    readonly UpdateInfo _u;

    public UpdateWindow(UpdateInfo u)
    {
        InitializeComponent();
        DataContext = MainViewModel.I;
        _u = u;
        TitleText.Text = Loc.I.F("updTitle", u.Version);
        DescText.Text = Loc.I.F("updDesc", UpdateService.Current);
    }

    async void Install_Click(object s, RoutedEventArgs e)
    {
        InstallBtn.IsEnabled = LaterBtn.IsEnabled = false;
        Bar.Visibility = StatusText.Visibility = Visibility.Visible;
        try
        {
            await UpdateService.InstallAsync(_u, new Progress<double>(p => { Bar.Value = p; StatusText.Text = Loc.I.F("downloading", p.ToString("P0")); }));
            App.RestartApp();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            InstallBtn.IsEnabled = LaterBtn.IsEnabled = true;
        }
    }

    void Drag(object s, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}
