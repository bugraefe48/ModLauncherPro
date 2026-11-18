using System.IO;
using System.Text.Json;
using System.Windows;
using Wpf.Ui.Controls;

namespace ModLauncherPro;

public partial class SettingsWindow : FluentWindow
{
    readonly AppData data;
    readonly AppSettings settings;
    readonly Action onChanged;
    readonly Action onModesChanged;
    readonly List<BrowserInfo> browsers = new();
    bool loading = true;

    public SettingsWindow(AppData data, Action onChanged, Action onModesChanged)
    {
        InitializeComponent();
        this.data = data;
        settings = data.Settings;
        this.onChanged = onChanged;
        this.onModesChanged = onModesChanged;

        LanguageBox.SelectedIndex = (int)settings.Language;
        ThemeBox.SelectedIndex = (int)settings.Theme;

        browsers.Add(new BrowserInfo("Windows Default", ""));
        browsers.AddRange(BrowserFinder.Find());

        var saved = settings.DefaultBrowserPath;
        if (!string.IsNullOrWhiteSpace(saved) &&
            !browsers.Any(b => b.Path.Equals(saved, StringComparison.OrdinalIgnoreCase)))
        {
            browsers.Add(CustomBrowser(saved));
        }
        RefreshBrowsers();
        BrowserBox.SelectedValue = saved ?? "";

        ConfirmBox.IsChecked = settings.ConfirmBeforeRun;
        DelaySlider.Value = settings.DelayBetweenActionsMs;
        DelayText.Text = $"{settings.DelayBetweenActionsMs} ms";
        TrayBox.IsChecked = settings.MinimizeToTray;
        StartupBox.IsChecked = settings.StartWithWindows;

        loading = false;
    }

    static BrowserInfo CustomBrowser(string path) =>
        new("Custom: " + Path.GetFileName(path), path);

    void RefreshBrowsers()
    {
        BrowserBox.ItemsSource = null;
        BrowserBox.ItemsSource = browsers;
    }

    void LanguageBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (loading) return;
        settings.Language = (AppLanguage)LanguageBox.SelectedIndex;
        Loc.Instance.CurrentLanguage = settings.Language;
        onChanged();
    }

    void ThemeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (loading) return;
        settings.Theme = (AppTheme)ThemeBox.SelectedIndex;
        onChanged();
    }

    void BrowserBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (loading || BrowserBox.SelectedValue is not string path) return;
        settings.DefaultBrowserPath = path;
        onChanged();
    }

    void BrowseBrowser_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Browser",
            Filter = "Applications (*.exe)|*.exe"
        };
        if (dialog.ShowDialog() != true) return;

        loading = true;
        if (!browsers.Any(b => b.Path.Equals(dialog.FileName, StringComparison.OrdinalIgnoreCase)))
            browsers.Add(CustomBrowser(dialog.FileName));
        RefreshBrowsers();
        BrowserBox.SelectedValue = dialog.FileName;
        loading = false;

        settings.DefaultBrowserPath = dialog.FileName;
        onChanged();
    }

    void Confirm_Changed(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        settings.ConfirmBeforeRun = ConfirmBox.IsChecked == true;
        onChanged();
    }

    void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (loading) return;
        int ms = (int)DelaySlider.Value;
        settings.DelayBetweenActionsMs = ms;
        if (DelayText != null)
            DelayText.Text = $"{ms} ms";
        onChanged();
    }

    void Tray_Changed(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        settings.MinimizeToTray = TrayBox.IsChecked == true;
        onChanged();
    }

    void Startup_Changed(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        settings.StartWithWindows = StartupBox.IsChecked == true;
        onChanged();
    }

    void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = "modes.json"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Storage.Export(dialog.FileName, data.Modes);
            System.Windows.MessageBox.Show("Exported!", "Mod Launcher",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var imported = Storage.Import(dialog.FileName);
            foreach (var mode in imported)
            {
                mode.Id = Guid.NewGuid();
                data.Modes.Add(mode);
            }
            onModesChanged();
            System.Windows.MessageBox.Show($"{imported.Count} modes imported.", "Mod Launcher",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}