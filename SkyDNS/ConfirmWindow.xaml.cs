using System.Windows;
using System.Windows.Input;

namespace SkyDNS;

/// <summary>Themed delete confirmation; ShowDialog() returns true when confirmed.</summary>
public partial class ConfirmWindow : Window
{
    public ConfirmWindow(string message)
    {
        InitializeComponent();
        DataContext = MainViewModel.I;
        MessageText.Text = message;
        Loaded += (_, _) => CancelBtn.Focus(); // safe default, like the old MessageBox (No)
    }

    void Ok_Click(object s, RoutedEventArgs e) => DialogResult = true;
    void Drag(object s, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}
