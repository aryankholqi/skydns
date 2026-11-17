using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace SkyDNS;

public partial class App : Application
{
    Mutex? _mutex;
    EventWaitHandle? _showEvent;
    static TrayService? _tray;
    static MainWindow? _main;
    static WidgetWindow? _widget;

    public static bool IsExiting { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Single instance: a second launch just brings the first window forward.
        _mutex = new Mutex(true, "SkyDNS_SingleInstance", out bool first);
        if (!first)
        {
            try { EventWaitHandle.OpenExisting("SkyDNS_Show").Set(); } catch { }
            Shutdown();
            return;
        }
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "SkyDNS_Show");
        ThreadPool.RegisterWaitForSingleObject(_showEvent, (_, _) => Dispatcher.BeginInvoke(new Action(ShowMain)), null, Timeout.Infinite, false);

        base.OnStartup(e);
        DispatcherUnhandledException += (_, a) => { MessageBox.Show(a.Exception.Message, "SkyDNS"); a.Handled = true; };

        var vm = MainViewModel.I;
        vm.Error += msg => MessageBox.Show(msg, Loc.I["appName"], MessageBoxButton.OK, MessageBoxImage.Warning,
            MessageBoxResult.OK, Loc.I.IsRtl ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : MessageBoxOptions.None);
        vm.DetectActive();

        _tray = new TrayService();
        if (!e.Args.Contains("--tray")) ShowMain();
        if (vm.Settings.WidgetVisible) ToggleWidget(true);
        if (vm.Settings.PingOnLaunch) await vm.TestAllAsync();
    }

    public static void ShowMain()
    {
        _main ??= new MainWindow();
        _main.Show();
        if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    public static void ToggleWidget(bool? show = null)
    {
        bool visible = show ?? !(_widget?.IsVisible ?? false);
        if (visible) { _widget ??= new WidgetWindow(); _widget.Show(); }
        else _widget?.Hide();
        MainViewModel.I.Settings.WidgetVisible = visible;
        MainViewModel.I.Save();
    }

    public static async void ExitApp()
    {
        if (IsExiting) return;
        IsExiting = true;
        var vm = MainViewModel.I;
        if (vm.Settings.RestoreOnExit && vm.AnyActive) await vm.DisconnectAsync();
        _tray?.Dispose();
        Current.Shutdown();
    }
}
