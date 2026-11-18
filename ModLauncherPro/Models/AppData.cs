using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic.Logging;

namespace ModLauncherPro;

public enum AppTheme
{
    System,
    Dark,
    Light
}

public enum AppLanguage
{
    Turkish,
    English
}

// Yapılacaklar (To-Do) öğesi
public class TodoItem : NotifyBase
{
    string title = "";
    bool isDone;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => title;
        set { title = value; OnPropertyChanged(); }
    }

    public bool IsDone
    {
        get => isDone;
        set { isDone = value; OnPropertyChanged(); }
    }
}

// Takvim / Sınav etkinliği
public class CalendarEvent : NotifyBase
{
    string title = "";
    DateTime date = DateTime.Today;
    string category = "Sınav / Exam";

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => title;
        set { title = value; OnPropertyChanged(); OnPropertyChanged(nameof(DaysLeftText)); }
    }

    public DateTime Date
    {
        get => date;
        set { date = value; OnPropertyChanged(); OnPropertyChanged(nameof(DateString)); OnPropertyChanged(nameof(DaysLeftText)); }
    }

    public string Category
    {
        get => category;
        set { category = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string DateString => Date.ToString("dd.MM.yyyy");

    [JsonIgnore]
    public string DaysLeftText
    {
        get
        {
            int days = (Date.Date - DateTime.Today).Days;
            bool isEn = Loc.Instance.CurrentLanguage == AppLanguage.English;
            if (days == 0) return isEn ? "Today!" : "Bugün!";
            if (days > 0) return isEn ? $"{days} days left" : $"{days} gün kaldı";
            return isEn ? $"{Math.Abs(days)} days ago" : $"{Math.Abs(days)} gün geçti";
        }
    }
}

public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public AppLanguage Language { get; set; } = AppLanguage.Turkish;
    public string DefaultBrowserPath { get; set; } = "";
    public bool ConfirmBeforeRun { get; set; } = false;
    public int DelayBetweenActionsMs { get; set; } = 500;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public int TimerAlertMinutes { get; set; } = 5; // Süre bitmeden kaç dk önce uyarsın
}

public class AppData
{
    public AppSettings Settings { get; set; } = new();
    public ObservableCollection<Mode> Modes { get; set; } = new();
    public ObservableCollection<ModeAction> StartupActions { get; set; } = new(); // Masaüstü açıldığında çalışacaklar
    public ObservableCollection<ModeAction> Favorites { get; set; } = new();      // Favori uygulama ve siteler
    public ObservableCollection<TodoItem> TodoItems { get; set; } = new();        // Yapılacaklar listesi
    public ObservableCollection<CalendarEvent> CalendarEvents { get; set; } = new(); // Sınav ve etkinlik takvimi
}