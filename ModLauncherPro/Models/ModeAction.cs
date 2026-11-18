using System.IO;
using System.Text.Json.Serialization;

namespace ModLauncherPro;

public enum ActionType
{
    App,        // Program aç
    Url,        // Web sitesi aç
    CloseApp    // Çalışan programı kapat
}

public class ModeAction : NotifyBase
{
    ActionType type = ActionType.App;
    string name = "";
    string target = "";
    string arguments = "";
    string browserPath = "";
    bool forceClose;

    public ActionType Type
    {
        get => type;
        set { type = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); OnPropertyChanged(nameof(Icon)); }
    }

    // Favorilerde veya başlangıçta özel isim vermek istersek (boşsa dosya/site adını gösterir)
    public string CustomName
    {
        get => name;
        set { name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    public string Target
    {
        get => target;
        set { target = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    public string Arguments
    {
        get => arguments;
        set { arguments = value; OnPropertyChanged(); }
    }

    public string BrowserPath
    {
        get => browserPath;
        set { browserPath = value; OnPropertyChanged(); }
    }

    public bool ForceClose
    {
        get => forceClose;
        set { forceClose = value; OnPropertyChanged(); }
    }

    public static string ProcessNameOf(string target) =>
        string.IsNullOrWhiteSpace(target) ? "" : Path.GetFileNameWithoutExtension(target.Trim());

    [JsonIgnore]
    public string Icon => Type switch
    {
        ActionType.Url => "🌐",
        ActionType.CloseApp => "⛔",
        _ => "🖥️"
    };

    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CustomName))
                return CustomName;

            return Type switch
            {
                ActionType.Url => string.IsNullOrWhiteSpace(Target) ? "Web Sitesi" : Target,
                ActionType.CloseApp => "Kapat: " + ProcessNameOf(Target),
                _ => string.IsNullOrWhiteSpace(Target) ? "Uygulama" : ProcessNameOf(Target)
            };
        }
    }
}