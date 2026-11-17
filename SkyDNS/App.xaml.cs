using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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

        UpdateService.Cleanup();
        // the app lives in the tray for days, so check at launch and then every 12 hours
        new DispatcherTimer(TimeSpan.FromHours(12), DispatcherPriority.Background, (_, _) => AutoCheck(), Dispatcher).Start();
        AutoCheck();
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

    static void AutoCheck() { if (MainViewModel.I.Settings.CheckUpdates) _ = CheckForUpdateAsync(false); }

    static Version? _offered;

    /// <summary>Auto checks stay silent on errors and offer each version once per session; manual checks always show.</summary>
    public static async Task CheckForUpdateAsync(bool manual)
    {
        var vm = MainViewModel.I;
        vm.UpdateStatus = Loc.I["updChecking"];
        UpdateInfo? u;
        try { u = await UpdateService.CheckAsync(); }
        catch { vm.UpdateStatus = Loc.I["updFail"]; return; }
        vm.UpdateStatus = u == null ? Loc.I["updLatest"] : Loc.I.F("updAvail", u.Version);
        if (u == null || IsExiting || (!manual && u.Version == _offered)) return;
        _offered = u.Version;
        var w = new UpdateWindow(u);
        var owner = Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive && x is not UpdateWindow) ?? (_main is { IsVisible: true } ? _main : null);
        if (owner != null) w.Owner = owner; else w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        w.ShowDialog();
    }

    /// <summary>Run the installer silently (it relaunches the app when done) and quit so it can replace our files.
    /// Skips RestoreOnExit since the app comes straight back.</summary>
    public static void ExitForUpdate(string setup)
    {
        Process.Start(new ProcessStartInfo(setup, "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS") { UseShellExecute = true });
        IsExiting = true;
        _tray?.Dispose();
        Current.Shutdown();
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
