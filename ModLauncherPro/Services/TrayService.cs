namespace ModLauncherPro;

public class TrayService : IDisposable
{
    readonly System.Windows.Forms.NotifyIcon icon;
    readonly AppData data;
    readonly Action showWindow;
    readonly Action exitApp;

    public TrayService(AppData data, Action showWindow, Action exitApp)
    {
        this.data = data;
        this.showWindow = showWindow;
        this.exitApp = exitApp;

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Opening += (_, _) => BuildMenu(menu);

        icon = new System.Windows.Forms.NotifyIcon
        {
            Icon = (Environment.ProcessPath != null ? System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath) : null)
                   ?? System.Drawing.SystemIcons.Application,
            Text = "Mod Launcher Pro",
            ContextMenuStrip = menu,
            Visible = true
        };

        icon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                showWindow();
        };
    }

    void BuildMenu(System.Windows.Forms.ContextMenuStrip menu)
    {
        bool en = Loc.Instance.CurrentLanguage == AppLanguage.English;
        menu.Items.Clear();
        menu.Items.Add(en ? "Open Mod Launcher" : "Mod Launcher'ı Aç", null, (_, _) => showWindow());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        if (data.Modes.Count == 0)
        {
            var empty = menu.Items.Add(en ? "(No modes yet)" : "(Henüz mod yok)");
            empty.Enabled = false;
        }

        foreach (var mode in data.Modes.ToList())
        {
            menu.Items.Add("▶ " + mode.Name, null, async (_, _) => await RunAsync(mode));
        }

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(en ? "Exit" : "Çıkış", null, (_, _) => exitApp());
    }

    public async Task RunAsync(Mode mode)
    {
        var errors = await ModeRunner.RunAsync(mode, data.Settings);
        if (errors.Count > 0)
            Notify(Loc.Instance.CurrentLanguage == AppLanguage.English ? "Some actions failed" : "Bazı eylemler çalışmadı", string.Join("\n", errors));
    }

    public void Notify(string title, string text, System.Windows.Forms.ToolTipIcon tipIcon = System.Windows.Forms.ToolTipIcon.Info)
    {
        icon.ShowBalloonTip(4000, title, text, tipIcon);
    }

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
    }
}