using System.Diagnostics;
using System.IO;

namespace ModLauncherPro;

public static class ModeRunner
{
    public static Task<List<string>> RunAsync(Mode mode, AppSettings settings) =>
        RunActionsAsync(mode.Actions, settings);

    public static async Task<List<string>> RunActionsAsync(IList<ModeAction> actions, AppSettings settings)
    {
        var errors = new List<string>();

        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var error = await TryExecuteAsync(action, settings);
            if (error != null)
                errors.Add($"{action.DisplayName}: {error}");

            bool isLast = i == actions.Count - 1;
            if (!isLast && settings.DelayBetweenActionsMs > 0)
                await Task.Delay(settings.DelayBetweenActionsMs);
        }

        return errors;
    }

    public static async Task<string?> TryExecuteAsync(ModeAction action, AppSettings settings)
    {
        try
        {
            await ExecuteAsync(action, settings);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    static async Task ExecuteAsync(ModeAction action, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
            throw new InvalidOperationException("Hedef boş.");

        switch (action.Type)
        {
            case ActionType.App:
                StartApp(action);
                break;
            case ActionType.Url:
                StartUrl(action, settings);
                break;
            case ActionType.CloseApp:
                await CloseAppAsync(action);
                break;
        }
    }

    static void StartApp(ModeAction action)
    {
        if (!File.Exists(action.Target))
            throw new FileNotFoundException("Dosya bulunamadı: " + action.Target);

        Process.Start(new ProcessStartInfo
        {
            FileName = action.Target,
            Arguments = action.Arguments,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(action.Target) ?? ""
        });
    }

    static void StartUrl(ModeAction action, AppSettings settings)
    {
        var text = action.Target.Trim();
        if (!text.Contains("://"))
            text = "https://" + text;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Geçerli bir web adresi değil.");
        }

        var url = uri.AbsoluteUri;
        var browser = !string.IsNullOrWhiteSpace(action.BrowserPath)
            ? action.BrowserPath
            : settings.DefaultBrowserPath;

        if (!string.IsNullOrWhiteSpace(browser) && File.Exists(browser))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = browser,
                Arguments = $"\"{url}\"",
                UseShellExecute = true
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }

    static async Task CloseAppAsync(ModeAction action)
    {
        var name = ModeAction.ProcessNameOf(action.Target);
        var processes = Process.GetProcessesByName(name);
        if (processes.Length == 0) return;

        try
        {
            var windowed = processes.Where(p => p.MainWindowHandle != IntPtr.Zero).ToList();
            foreach (var p in windowed)
                p.CloseMainWindow();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                foreach (var p in windowed)
                    await p.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) { }

            if (action.ForceClose)
            {
                foreach (var p in processes.Where(p => !p.HasExited))
                    p.Kill(entireProcessTree: true);
            }
        }
        finally
        {
            foreach (var p in processes)
                p.Dispose();
        }
    }
}