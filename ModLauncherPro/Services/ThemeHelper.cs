using System.Windows;
using Wpf.Ui.Appearance;

namespace ModLauncherPro;

public static class ThemeHelper
{
    public static void Apply(AppTheme theme, Window window)
    {
        switch (theme)
        {
            case AppTheme.Dark:
                SystemThemeWatcher.UnWatch(window);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;

            case AppTheme.Light:
                SystemThemeWatcher.UnWatch(window);
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;

            default:
                ApplicationThemeManager.ApplySystemTheme();
                SystemThemeWatcher.Watch(window);
                break;
        }
    }
}