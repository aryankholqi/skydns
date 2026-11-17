using System.Windows;
using System.Windows.Input;

namespace SkyDNS;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        MainViewModel.I.RefreshAdapters();
        DataContext = MainViewModel.I;
    }

    void Drag(object s, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
    void Close_Click(object s, RoutedEventArgs e) => Close();
    void CheckNow_Click(object s, RoutedEventArgs e) => _ = App.CheckForUpdateAsync(true);
}
