using System.Collections.ObjectModel;

namespace ModLauncherPro;

public record ModeColor(string Name, string Value);

public static class ModeColors
{
    // Modern ve estetik geniş renk paleti
    public static readonly List<ModeColor> All = new()
    {
        new("Kırmızı / Red", "#E74C3C"),
        new("Koyu Kırmızı / Dark Red", "#C0392B"),
        new("Turuncu / Orange", "#E67E22"),
        new("Koyu Turuncu / Dark Orange", "#D35400"),
        new("Sarı / Yellow", "#F1C40F"),
        new("Altın / Gold", "#F39C12"),
        new("Açık Yeşil / Light Green", "#2ECC71"),
        new("Yeşil / Green", "#27AE60"),
        new("Turkuaz / Cyan", "#1ABC9C"),
        new("Deniz Mavisi / Teal", "#16A085"),
        new("Mavi / Blue", "#3498DB"),
        new("Koyu Mavi / Dark Blue", "#2980B9"),
        new("Mor / Purple", "#9B59B6"),
        new("Koyu Mor / Dark Purple", "#8E44AD"),
        new("Pembe / Pink", "#E84393"),
        new("Açık Pembe / Light Pink", "#FD79A8"),
        new("Gümüş / Silver", "#BDC3C7"),
        new("Gri / Gray", "#7F8C8D")
    };
}

public class Mode : NotifyBase
{
    string name = "";
    string hotkey = "";
    string color = "#3498DB";

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name
    {
        get => name;
        set { name = value; OnPropertyChanged(); }
    }

    public string Hotkey
    {
        get => hotkey;
        set { hotkey = value; OnPropertyChanged(); }
    }

    public string Color
    {
        get => color;
        set { color = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ModeAction> Actions { get; set; } = new();
}