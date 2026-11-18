using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ModLauncherPro;

public static class SystemControlService
{
    // Windows CoreAudio COM arayüzleri ile doğrudan ana ses seviyesini kontrol eder
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorComObject { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int NotImpl1();
        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out int pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute(out bool pbMute);
    }

    static IAudioEndpointVolume? GetVolumeObject()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice device);
            Guid iid = typeof(IAudioEndpointVolume).GUID;
            device.Activate(ref iid, 23, IntPtr.Zero, out object vol);
            return (IAudioEndpointVolume)vol;
        }
        catch { return null; }
    }

    public static double GetMasterVolume()
    {
        var vol = GetVolumeObject();
        if (vol == null) return 50;
        vol.GetMasterVolumeLevelScalar(out float level);
        return Math.Round(level * 100);
    }

    public static void SetMasterVolume(double percent)
    {
        var vol = GetVolumeObject();
        if (vol == null) return;
        float scalar = (float)Math.Clamp(percent / 100.0, 0.0, 1.0);
        Guid guid = Guid.Empty;
        vol.SetMasterVolumeLevelScalar(scalar, ref guid);
    }

    public static void ToggleMute()
    {
        var vol = GetVolumeObject();
        if (vol == null) return;
        vol.GetMute(out bool muted);
        Guid guid = Guid.Empty;
        vol.SetMute(!muted, ref guid);
    }

    // Wi-Fi Adaptörünü Aç / Kapat
    public static void SetWifiEnabled(bool enabled)
    {
        try
        {
            string state = enabled ? "enabled" : "disabled";
            Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"interface set interface \"Wi-Fi\" {state}",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch
        {
            OpenUri("ms-settings:network-wifi");
        }
    }

    // Bluetooth Servisini Aç / Kapat veya Ayarları Aç
    public static void SetBluetoothEnabled(bool enabled)
    {
        try
        {
            string status = enabled ? "On" : "Off";
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-WindowStyle Hidden -Command \"Start-Service bthserv -ErrorAction SilentlyContinue\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            OpenUri("ms-settings:bluetooth");
        }
        catch
        {
            OpenUri("ms-settings:bluetooth");
        }
    }

    public static void OpenUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
        }
        catch { }
    }

    // Uygulama İçi Tıklanabilir (Click-Through) Sıcak Gece Işığı Filtresi (Tüm Ekranı Korur)
    static Window? dimmerWindow;

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    public static void SetScreenNightFilter(bool enabled, double opacity = 0.25)
    {
        if (!enabled)
        {
            dimmerWindow?.Close();
            dimmerWindow = null;
            return;
        }

        if (dimmerWindow == null)
        {
            dimmerWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 140, 40)),
                Opacity = opacity,
                Topmost = true,
                ShowInTaskbar = false,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight,
                IsHitTestVisible = false
            };

            dimmerWindow.SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(dimmerWindow).Handle;
                int exStyle = GetWindowLong(hwnd, -20);
                // WS_EX_TRANSPARENT (0x20) | WS_EX_TOOLWINDOW (0x80) -> Tıklamalar filtrenin arkasına geçer
                SetWindowLong(hwnd, -20, exStyle | 0x00000020 | 0x00000080);
            };
            dimmerWindow.Show();
        }
        else
        {
            dimmerWindow.Opacity = opacity;
        }
    }
}