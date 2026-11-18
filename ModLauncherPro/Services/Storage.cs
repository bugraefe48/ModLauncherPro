using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModLauncherPro;

public static class Storage
{
    static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ModLauncherPro");

    public static readonly string FilePath = Path.Combine(Folder, "data.json");

    static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppData Load()
    {
        if (!File.Exists(FilePath))
        {
            var fresh = CreateDefault();
            Save(fresh);
            return fresh;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<AppData>(json, Options);
            return data == null ? CreateDefault() : Sanitize(data);
        }
        catch (JsonException)
        {
            File.Copy(FilePath, FilePath + ".bak", overwrite: true);
            return CreateDefault();
        }
    }

    public static void Save(AppData data)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    public static void Export(string path, IEnumerable<Mode> modes)
    {
        var export = new AppData();
        foreach (var mode in modes)
            export.Modes.Add(mode);
        File.WriteAllText(path, JsonSerializer.Serialize(export, Options));
    }

    public static List<Mode> Import(string path)
    {
        var json = File.ReadAllText(path);
        var imported = JsonSerializer.Deserialize<AppData>(json, Options);
        return imported == null ? new List<Mode>() : Sanitize(imported).Modes.ToList();
    }

    static AppData Sanitize(AppData data)
    {
        data.Settings ??= new AppSettings();
        data.Modes ??= new ObservableCollection<Mode>();
        data.StartupActions ??= new ObservableCollection<ModeAction>();
        data.Favorites ??= new ObservableCollection<ModeAction>();
        data.TodoItems ??= new ObservableCollection<TodoItem>();
        data.CalendarEvents ??= new ObservableCollection<CalendarEvent>();

        Loc.Instance.CurrentLanguage = data.Settings.Language;
        return data;
    }

    static AppData CreateDefault() => new()
    {
        Modes =
        {
            new Mode
            {
                Name = "Ders & Çalışma Modu",
                Color = "#3498DB",
                Actions = { new ModeAction { Type = ActionType.Url, CustomName = "YouTube", Target = "https://www.youtube.com" } }
            },
            new Mode
            {
                Name = "Oyun Modu",
                Color = "#9B59B6"
            }
        },
        Favorites =
        {
            new ModeAction { Type = ActionType.Url, CustomName = "GitHub", Target = "https://github.com" },
            new ModeAction { Type = ActionType.Url, CustomName = "YouTube", Target = "https://www.youtube.com" }
        },
        TodoItems =
        {
            new TodoItem { Title = "Mod Launcher'a favori uygulamalarını ekle" }
        },
        CalendarEvents =
        {
            new CalendarEvent { Title = "Örnek Sınav / Etkinlik", Date = DateTime.Today.AddDays(7), Category = "Sınav" }
        }
    };
}