using System;
using System.ComponentModel;
using System.Linq;

namespace ModLauncherPro
{
    public class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new Loc();

        public event PropertyChangedEventHandler? PropertyChanged;

        private AppLanguage _currentLanguage = Enum.GetValues(typeof(AppLanguage))
            .Cast<AppLanguage>()
            .FirstOrDefault(l => l.ToString().StartsWith("T", StringComparison.OrdinalIgnoreCase));

        public AppLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage.Equals(value)) return;
                _currentLanguage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }

        public bool IsTurkish =>
            _currentLanguage.ToString().StartsWith("T", StringComparison.OrdinalIgnoreCase);

        private string T(string tr, string en) => IsTurkish ? tr : en;

        // ---------- Ayarlar ----------
        public string SettingsHeader => T("Ayarlar", "Settings");
        public string LblLanguage => T("Dil", "Language");
        public string LblTheme => T("Tema", "Theme");
        public string ThemeSystem => T("Sistem", "System");
        public string ThemeDark => T("Koyu", "Dark");
        public string ThemeLight => T("Açık", "Light");
        public string LblDefaultBrowser => T("Varsayılan tarayıcı", "Default browser");
        public string BtnBrowse => T("Gözat", "Browse");
        public string ChkConfirmRun => T("Modu çalıştırmadan önce onay iste", "Ask for confirmation before running a mode");
        public string LblDelay => T("Uygulamalar arası gecikme: ", "Delay between apps: ");
        public string ChkTray => T("Sistem tepsisine küçült", "Minimize to system tray");
        public string ChkStartup => T("Windows ile başlat", "Start with Windows");
        public string LblBackup => T("Yedekleme", "Backup");
        public string BtnExport => T("Dışa aktar", "Export");
        public string BtnImport => T("İçe aktar", "Import");

        // ---------- Genel / Karşılama ----------
        public string BtnHowToUse => T("Nasıl Kullanılır?", "How to Use");
        public string BtnSettings => T("Ayarlar", "Settings");
        public string WelcomeSubtitle => T("Tek tuşla çalışma ve oyun ortamını başlat.", "Start your work or gaming setup with one click.");
        public string BtnStart => T("Başla", "Start");
        public string HowToUseHeader => T("Nasıl Kullanılır?", "How to Use");

        // ---------- Sol menü ----------
        public string NavHome => T("🏠 Ana Menü", "🏠 Home");
        public string NavModes => T("📁 Modlar", "📁 Modes");
        public string NavStartup => T("🚀 Masaüstü Başlangıç", "🚀 Desktop Startup");
        public string NavFavorites => T("⭐ Favoriler", "⭐ Favorites");
        public string NavClock => T("⏱️ Saat", "⏱️ Clock");
        public string NavCalendar => T("📅 Takvim & Yapılacaklar", "📅 Calendar & To-Do");
        public string NavSystem => T("🎛️ Hızlı Kontrol", "🎛️ Quick Control");

        // ---------- Modlar ----------
        public string ModesHeader => T("Modlar", "Modes");
        public string BtnNewMode => T("+ Yeni Mod", "+ New Mode");
        public string EmptyModeHint => T("Soldan bir mod veya eylem seç.", "Select a mode or action on the left.");
        public string LblModeName => T("Mod Adı", "Mode Name");
        public string LblColor => T("Renk", "Color");
        public string LblHotkey => T("Kısayol Tuşu", "Hotkey");
        public string BtnClear => T("Temizle", "Clear");
        public string HotkeyHint => T("Kutuya tıkla ve bir tuş kombinasyonuna bas.", "Click the box and press a key combination.");
        public string BtnRunMode => T("▶ Modu Çalıştır", "▶ Run Mode");
        public string BtnAddApp => T("+ Uygulama", "+ App");
        public string BtnAddUrl => T("🌐 + Site", "🌐 + Site");
        public string BtnAddCloseApp => T("✕ + Kapatılacak Uygulama", "✕ + App to Close");
        public string BtnMoveUp => T("⬆ Yukarı", "⬆ Up");
        public string BtnMoveDown => T("⬇ Aşağı", "⬇ Down");
        public string BtnDelete => T("🗑 Sil", "🗑 Delete");
        public string LblTarget => T("Hedef (yol / adres)", "Target (path / address)");
        public string LblArgs => T("Argümanlar", "Arguments");
        public string LblBrowser => T("Tarayıcı yolu", "Browser path");
        public string ChkForceClose => T("Zorla kapat", "Force close");
        public string BtnTestAction => T("Test Et", "Test");

        // ---------- Masaüstü başlangıç ----------
        public string StartupTitle => T("Masaüstü Başlangıç", "Desktop Startup");
        public string StartupDesc => T("Bilgisayar açıldığında otomatik başlayacak uygulama ve siteler.", "Apps and sites that start automatically when your PC boots.");
        public string BtnRunAllStartup => T("▶ Hepsini Çalıştır", "▶ Run All");

        // ---------- Favoriler ----------
        public string FavoritesTitle => T("Favoriler", "Favorites");
        public string FavoritesDesc => T("Sık kullandığın uygulama ve siteler.", "Your frequently used apps and sites.");

        // ---------- Saat ----------
        public string TimerTitle => T("⏳ Zamanlayıcı", "⏳ Timer");
        public string LblHours => T("Saat", "Hours");
        public string LblMinutes => T("Dakika", "Minutes");
        public string LblSeconds => T("Saniye", "Seconds");
        public string LblAlertBefore => T("Bitmeden önce uyar:", "Alert before end:");
        public string BtnStartTimer => T("▶ Başlat", "▶ Start");
        public string BtnPause => T("⏸ Duraklat", "⏸ Pause");
        public string BtnReset => T("↺ Sıfırla", "↺ Reset");
        public string StopwatchTitle => T("⏱ Kronometre", "⏱ Stopwatch");
        public string BtnLap => T("🏁 Tur", "🏁 Lap");

        // ---------- Takvim & Yapılacaklar ----------
        public string CalendarTitle => T("📅 Sınav & Etkinlik Takvimi", "📅 Exam & Event Calendar");
        public string BtnAddEvent => T("+ Ekle", "+ Add");
        public string TodoTitle => T("✅ Yapılacaklar", "✅ To-Do");
        public string LblNewTodo => T("Yeni görev...", "New task...");
        public string BtnAddTodo => T("+ Ekle", "+ Add");

        // ---------- Hızlı kontrol ----------
        public string SysControlTitle => T("Hızlı Sistem Kontrolü", "Quick System Control");
        public string SysVolume => T("🔊 Ses", "🔊 Volume");
        public string BtnMute => T("🔇 Sessize Al", "🔇 Mute");
        public string SysWifi => T("📶 Wi-Fi", "📶 Wi-Fi");
        public string SysBluetooth => T("🔵 Bluetooth", "🔵 Bluetooth");
        public string BtnEnable => T("Aç", "Enable");
        public string BtnDisable => T("Kapat", "Disable");
        public string BtnOpenWinSettings => T("Windows Ayarları", "Windows Settings");
        public string SysNightLight => T("🌙 Gece Işığı / Göz Koruma", "🌙 Night Light / Eye Care");
        public string ChkScreenDimmer => T("Sıcak ekran filtresini aç", "Enable warm screen filter");
    }
}