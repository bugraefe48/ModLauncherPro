using System.Threading;
using System.Windows;
using ModLauncherPro;

namespace ModLauncherPro;

public partial class App : Application
{
    static Mutex? mutex;
    static EventWaitHandle? showEvent;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Beklenmeyen bir hata oluştu:\n\n" + args.Exception.Message,
                "Mod Launcher Pro", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "ModLauncherPro_ShowEvent");
        showEvent = evt;
        mutex = new Mutex(true, "ModLauncherPro_SingleInstance", out bool createdNew);

            if (!createdNew)
        {
            evt.Set();
            Shutdown();
            return;
        }

        bool launchedAtLogin = e.Args.Contains("--tray");

        var window = new MainWindow();
        MainWindow = window;

        if (!(launchedAtLogin && window.StartHidden))
            window.Show();

        // Bilgisayar ilk açıldığında Masaüstü Başlangıç (StartupActions) öğelerini otomatik çalıştır
        if (launchedAtLogin)
            await window.RunStartupItemsAsync();

        var listener = new Thread(() =>
        {
            while (evt.WaitOne())
            {
                try { Dispatcher.Invoke(() => window.ShowWindow()); }
                catch (Exception) { return; }
            }
        })
        { IsBackground = true };
        listener.Start();
    }
}