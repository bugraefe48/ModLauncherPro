using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace ModLauncherPro;

public partial class MainWindow : FluentWindow
{
    readonly AppData data;
    readonly TrayService tray;
    readonly HotkeyService hotkeys;
    object? selected;
    bool exiting;

    // Zamanlayıcı (Countdown) değişkenleri
    readonly DispatcherTimer countdownTimer;
    TimeSpan remainingTime = TimeSpan.FromHours(1);
    bool preAlertShown;

    // Kronometre (Stopwatch) değişkenleri
    readonly Stopwatch stopwatch = new();
    readonly DispatcherTimer stopwatchUiTimer;
    int lapCounter = 1;

    bool volumeLoading;

    public bool StartHidden => data.Settings.MinimizeToTray;

    public MainWindow()
    {
        InitializeComponent();

        data = Storage.Load();

        // HATA ÇÖZÜMÜ: Tema uygulama işlemi pencere tam olarak yüklendikten sonraya ertelendi
        Loaded += (_, _) => ThemeHelper.Apply(data.Settings.Theme, this);

        // Koleksiyon bağlantıları
        ModeTree.ItemsSource = data.Modes;

        // YENİ RENK PALETİ BAĞLANTISI
        ColorPaletteControl.ItemsSource = ModeColors.All;

        StartupList.ItemsSource = data.StartupActions;
        FavoritesGrid.ItemsSource = data.Favorites;
        CalendarEventsList.ItemsSource = data.CalendarEvents;
        TodoList.ItemsSource = data.TodoItems;
        EventCalendar.SelectedDate = DateTime.Today;

        if (data.Settings.StartWithWindows)
            StartupHelper.Set(true);

        tray = new TrayService(data, ShowWindow, ExitApp);

        hotkeys = new HotkeyService(OnHotkeyPressed);
        var failed = hotkeys.RegisterAll(data.Modes);
        if (failed.Count > 0)
        {
            tray.Notify("Kısayol Uyarısı",
                string.Join(", ", failed.Select(m => $"{m.Name} ({m.Hotkey})")));
        }

        // Geri sayım sayacı (1 saniyede bir tetiklenir)
        countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        countdownTimer.Tick += CountdownTimer_Tick;

        // Kronometre ekran güncelleyici (100 ms'de bir tetiklenir)
        stopwatchUiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        stopwatchUiTimer.Tick += (_, _) =>
        {
            StopwatchDisplay.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.f");
        };

        System.Windows.Application.Current.SessionEnding += (_, _) => exiting = true;
        System.Windows.Application.Current.Exit += (_, _) =>
        {
            SystemControlService.SetScreenNightFilter(false);
            hotkeys.Dispose();
            tray.Dispose();
        };

        Closing += OnWindowClosing;
        ShowPanel(null);
    }

    // ---------- Karşılama Ekranı & Navigasyon ----------

    void EnterDashboard_Click(object sender, RoutedEventArgs e)
    {
        WelcomeScreen.Visibility = Visibility.Collapsed;
        DashboardScreen.Visibility = Visibility.Visible;
    }

    void BackToWelcome_Click(object sender, RoutedEventArgs e)
    {
        DashboardScreen.Visibility = Visibility.Collapsed;
        WelcomeScreen.Visibility = Visibility.Visible;
    }

    void SwitchTab(UIElement activeView)
    {
        ViewModes.Visibility = Visibility.Collapsed;
        ViewStartup.Visibility = Visibility.Collapsed;
        ViewFavorites.Visibility = Visibility.Collapsed;
        ViewClock.Visibility = Visibility.Collapsed;
        ViewCalendar.Visibility = Visibility.Collapsed;
        ViewSystem.Visibility = Visibility.Collapsed;

        activeView.Visibility = Visibility.Visible;
    }

    void NavModes_Click(object sender, RoutedEventArgs e) => SwitchTab(ViewModes);
    void NavStartup_Click(object sender, RoutedEventArgs e) => SwitchTab(ViewStartup);
    void NavFavorites_Click(object sender, RoutedEventArgs e) => SwitchTab(ViewFavorites);
    void NavClock_Click(object sender, RoutedEventArgs e) => SwitchTab(ViewClock);
    void NavCalendar_Click(object sender, RoutedEventArgs e) => SwitchTab(ViewCalendar);

    void NavSystem_Click(object sender, RoutedEventArgs e)
    {
        volumeLoading = true;
        VolumeSlider.Value = SystemControlService.GetMasterVolume();
        VolumePercentText.Text = $"{VolumeSlider.Value:0}%";
        volumeLoading = false;
        SwitchTab(ViewSystem);
    }

    void OpenHelp_Click(object sender, RoutedEventArgs e) => HelpOverlay.Visibility = Visibility.Visible;
    void CloseHelp_Click(object sender, RoutedEventArgs e) => HelpOverlay.Visibility = Visibility.Collapsed;

    // ---------- Masaüstü Başlangıç (Auto-Start) İşlemleri ----------

    public async Task RunStartupItemsAsync()
    {
        if (data.StartupActions.Count == 0) return;
        await ModeRunner.RunActionsAsync(data.StartupActions, data.Settings);
    }

    void AddStartupApp_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Applications (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        data.StartupActions.Add(new ModeAction
        {
            Type = ActionType.App,
            CustomName = ModeAction.ProcessNameOf(dialog.FileName),
            Target = dialog.FileName
        });
        Storage.Save(data);
    }

    void AddStartupUrl_Click(object sender, RoutedEventArgs e)
    {
        data.StartupActions.Add(new ModeAction
        {
            Type = ActionType.Url,
            CustomName = "Web Site",
            Target = "https://www.google.com"
        });
        Storage.Save(data);
    }

    void RemoveStartupItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ModeAction item)
        {
            data.StartupActions.Remove(item);
            Storage.Save(data);
        }
    }

    async void RunAllStartup_Click(object sender, RoutedEventArgs e)
    {
        await RunStartupItemsAsync();
    }

    // ---------- Favoriler İşlemleri ----------

    void AddFavApp_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Applications (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        data.Favorites.Add(new ModeAction
        {
            Type = ActionType.App,
            CustomName = ModeAction.ProcessNameOf(dialog.FileName),
            Target = dialog.FileName
        });
        Storage.Save(data);
    }

    void AddFavUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = string.IsNullOrWhiteSpace(FavUrlInput.Text) ? "https://www.youtube.com" : FavUrlInput.Text.Trim();
        data.Favorites.Add(new ModeAction
        {
            Type = ActionType.Url,
            CustomName = url.Replace("https://", "").Replace("http://", "").Split('/')[0],
            Target = url
        });
        FavUrlInput.Text = "";
        Storage.Save(data);
    }

    void RemoveFavItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ModeAction item)
        {
            data.Favorites.Remove(item);
            Storage.Save(data);
        }
    }

    async void LaunchFavItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ModeAction item)
        {
            var err = await ModeRunner.TryExecuteAsync(item, data.Settings);
            if (err != null)
                MessageBox.Show(err, "Mod Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---------- Saat: Zamanlayıcı (Countdown) ve Kronometre (Stopwatch) ----------

    void QuickTimer_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string tag && int.TryParse(tag, out int totalMinutes))
        {
            TimerHoursBox.Text = (totalMinutes / 60).ToString();
            TimerMinutesBox.Text = (totalMinutes % 60).ToString();
            TimerSecondsBox.Text = "0";
            ResetTimer_Click(sender, e);
        }
    }

    void AlertMinutesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (data == null) return;
        if (AlertMinutesBox.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag && int.TryParse(tag, out int mins))
        {
            data.Settings.TimerAlertMinutes = mins;
            Storage.Save(data);
        }
    }

    void StartTimer_Click(object sender, RoutedEventArgs e)
    {
        if (!countdownTimer.IsEnabled)
        {
            if (remainingTime.TotalSeconds <= 0)
                ReadTimerInputs();
            countdownTimer.Start();
        }
    }

    void PauseTimer_Click(object sender, RoutedEventArgs e) => countdownTimer.Stop();

    void ResetTimer_Click(object sender, RoutedEventArgs e)
    {
        countdownTimer.Stop();
        ReadTimerInputs();
        preAlertShown = false;
    }

    void ReadTimerInputs()
    {
        int.TryParse(TimerHoursBox.Text, out int h);
        int.TryParse(TimerMinutesBox.Text, out int m);
        int.TryParse(TimerSecondsBox.Text, out int s);
        remainingTime = new TimeSpan(Math.Max(0, h), Math.Max(0, m), Math.Max(0, s));
        TimerDisplay.Text = remainingTime.ToString(@"hh\:mm\:ss");
    }

    void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        if (remainingTime.TotalSeconds <= 0)
        {
            countdownTimer.Stop();
            SystemSounds.Exclamation.Play();
            bool en = Loc.Instance.CurrentLanguage == AppLanguage.English;
            tray.Notify(
                en ? "⏰ Time is Up!" : "⏰ Süre Doldu!",
                en ? "Your countdown timer has finished." : "Belirlediğin zamanlayıcı süresi tamamlandı.");
            return;
        }

        remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
        TimerDisplay.Text = remainingTime.ToString(@"hh\:mm\:ss");

        int alertMins = data.Settings.TimerAlertMinutes;
        if (!preAlertShown && alertMins > 0 && remainingTime.TotalSeconds == alertMins * 60)
        {
            preAlertShown = true;
            SystemSounds.Asterisk.Play();
            bool en = Loc.Instance.CurrentLanguage == AppLanguage.English;
            tray.Notify(
                en ? $"⏳ {alertMins} Minutes Left!" : $"⏳ Bitmesine {alertMins} Dakika Kaldı!",
                en ? $"Only {alertMins} minutes remaining on your timer." : $"Zamanlayıcının bitmesine {alertMins} dakika kaldı.");
        }
    }

    void StartStopwatch_Click(object sender, RoutedEventArgs e)
    {
        stopwatch.Start();
        stopwatchUiTimer.Start();
    }

    void PauseStopwatch_Click(object sender, RoutedEventArgs e)
    {
        stopwatch.Stop();
        stopwatchUiTimer.Stop();
    }

    void LapStopwatch_Click(object sender, RoutedEventArgs e)
    {
        if (!stopwatch.IsRunning) return;
        LapList.Items.Insert(0, $"#{lapCounter++}   —   {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}");
    }

    void ResetStopwatch_Click(object sender, RoutedEventArgs e)
    {
        stopwatch.Reset();
        stopwatchUiTimer.Stop();
        StopwatchDisplay.Text = "00:00:00.0";
        LapList.Items.Clear();
        lapCounter = 1;
    }

    // ---------- Takvim & Yapılacaklar (To-Do) ----------

    void AddCalendarEvent_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EventTitleBox.Text)) return;
        var date = EventCalendar.SelectedDate ?? DateTime.Today;

        data.CalendarEvents.Add(new CalendarEvent
        {
            Title = EventTitleBox.Text.Trim(),
            Date = date
        });
        EventTitleBox.Text = "";
        Storage.Save(data);
    }

    void RemoveCalendarEvent_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CalendarEvent ev)
        {
            data.CalendarEvents.Remove(ev);
            Storage.Save(data);
        }
    }

    void AddTodo_Click(object sender, RoutedEventArgs e) => AddTodoFromInput();

    void TodoInputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddTodoFromInput();
    }

    void AddTodoFromInput()
    {
        if (string.IsNullOrWhiteSpace(TodoInputBox.Text)) return;
        data.TodoItems.Add(new TodoItem { Title = TodoInputBox.Text.Trim() });
        TodoInputBox.Text = "";
        Storage.Save(data);
    }

    void RemoveTodo_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is TodoItem item)
        {
            data.TodoItems.Remove(item);
            Storage.Save(data);
        }
    }

    // ---------- Hızlı Sistem Kontrolü (Ses, Wi-Fi, Bluetooth, Gece Işığı) ----------

    void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (volumeLoading || VolumePercentText == null) return;
        VolumePercentText.Text = $"{e.NewValue:0}%";
        SystemControlService.SetMasterVolume(e.NewValue);
    }

    void MuteVolume_Click(object sender, RoutedEventArgs e) => SystemControlService.ToggleMute();
    void WifiOn_Click(object sender, RoutedEventArgs e) => SystemControlService.SetWifiEnabled(true);
    void WifiOff_Click(object sender, RoutedEventArgs e) => SystemControlService.SetWifiEnabled(false);
    void WifiSettings_Click(object sender, RoutedEventArgs e) => SystemControlService.OpenUri("ms-settings:network-wifi");
    void BluetoothOn_Click(object sender, RoutedEventArgs e) => SystemControlService.SetBluetoothEnabled(true);
    void BluetoothSettings_Click(object sender, RoutedEventArgs e) => SystemControlService.OpenUri("ms-settings:bluetooth");
    void NightLightSettings_Click(object sender, RoutedEventArgs e) => SystemControlService.OpenUri("ms-settings:nightlight");

    void NightFilter_Changed(object sender, RoutedEventArgs e)
    {
        SystemControlService.SetScreenNightFilter(NightFilterCheck.IsChecked == true, NightFilterSlider.Value);
    }

    void NightFilterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NightFilterCheck?.IsChecked == true)
            SystemControlService.SetScreenNightFilter(true, e.NewValue);
    }

    // ---------- Mevcut Mod ve Kısayol İşlemleri ----------

    static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    async void OnHotkeyPressed(Mode mode) => await tray.RunAsync(mode);

    void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        if (selected is not Mode mode) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.None or Key.ImeProcessed)
            return;

        var modifiers = Keyboard.Modifiers;

        if (modifiers == ModifierKeys.None && (key is Key.Back or Key.Delete))
        {
            SetHotkey(mode, "");
            return;
        }

        if (modifiers == ModifierKeys.None) return;
        if (modifiers.HasFlag(ModifierKeys.Control) && modifiers.HasFlag(ModifierKeys.Alt)) return;

        SetHotkey(mode, HotkeyHelper.Format(modifiers, key));
    }

    void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (selected is Mode mode) SetHotkey(mode, "");
    }

    void SetHotkey(Mode mode, string text)
    {
        var old = mode.Hotkey;
        mode.Hotkey = text;

        var failed = hotkeys.RegisterAll(data.Modes);
        if (text != "" && failed.Contains(mode))
        {
            mode.Hotkey = old;
            hotkeys.RegisterAll(data.Modes);
            MessageBox.Show("Bu kısayol başka bir program tarafından kullanılıyor.",
                "Kısayol", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Storage.Save(data);
    }

    void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Storage.Save(data);
        if (!exiting && data.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ShowWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
    }

    void ExitApp()
    {
        exiting = true;
        Storage.Save(data);
        System.Windows.Application.Current.Shutdown();
    }

    void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(data, OnSettingsChanged, OnModesImported) { Owner = this };
        window.ShowDialog();
    }

    void OnSettingsChanged()
    {
        Storage.Save(data);
        ThemeHelper.Apply(data.Settings.Theme, this);
        StartupHelper.Set(data.Settings.StartWithWindows);
    }

    void OnModesImported()
    {
        Storage.Save(data);
        hotkeys.RegisterAll(data.Modes);
    }

    void ShowPanel(object? item)
    {
        selected = item;
        EmptyText.Visibility = Visibility.Visible;
        ModePanel.Visibility = Visibility.Collapsed;
        ActionPanel.Visibility = Visibility.Collapsed;

        if (item is Mode mode)
        {
            ModePanel.DataContext = mode;
            ModePanel.Visibility = Visibility.Visible;
            EmptyText.Visibility = Visibility.Collapsed;
        }
        else if (item is ModeAction action)
        {
            bool isUrl = action.Type == ActionType.Url;
            bool isClose = action.Type == ActionType.CloseApp;

            ActionPanel.DataContext = action;
            BrowseTargetButton.Visibility = Show(!isUrl);
            ArgsPanel.Visibility = Show(!isUrl && !isClose);
            BrowserPanel.Visibility = Show(isUrl);
            ForcePanel.Visibility = Show(isClose);

            ActionPanel.Visibility = Visibility.Visible;
            EmptyText.Visibility = Visibility.Collapsed;
        }
    }

    void ModeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        => ShowPanel(e.NewValue);

    void SelectMode(Mode mode)
    {
        ModeTree.UpdateLayout();
        if (ModeTree.ItemContainerGenerator.ContainerFromItem(mode) is System.Windows.Controls.TreeViewItem item)
        {
            item.IsSelected = true;
            item.BringIntoView();
        }
    }

    void SelectAction(Mode mode, ModeAction action)
    {
        ModeTree.UpdateLayout();
        if (ModeTree.ItemContainerGenerator.ContainerFromItem(mode) is System.Windows.Controls.TreeViewItem parent)
        {
            parent.IsExpanded = true;
            parent.UpdateLayout();
            if (parent.ItemContainerGenerator.ContainerFromItem(action) is System.Windows.Controls.TreeViewItem child)
            {
                child.IsSelected = true;
                child.BringIntoView();
            }
        }
    }

    string UniqueName(string baseName)
    {
        var name = baseName;
        int i = 2;
        while (data.Modes.Any(m => m.Name == name))
            name = $"{baseName} {i++}";
        return name;
    }

    void NewMode_Click(object sender, RoutedEventArgs e)
    {
        var mode = new Mode { Name = UniqueName("Yeni Mod") };
        data.Modes.Add(mode);
        Storage.Save(data);
        SelectMode(mode);
    }

    void DeleteMode_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not Mode mode) return;
        data.Modes.Remove(mode);
        hotkeys.RegisterAll(data.Modes);
        Storage.Save(data);
        ShowPanel(null);
    }

    // YENİ RENK SEÇİCİ PALET METODU
    void PaletteColor_Click(object sender, RoutedEventArgs e)
    {
        // Button kelimesinin başına System.Windows.Controls. ekledik
        if (selected is Mode mode && sender is System.Windows.Controls.Button btn && btn.Tag is string hex)
        {
            mode.Color = hex;
            Storage.Save(data);
        }
    }

    void MoveSelected(int delta)
    {
        if (selected is Mode mode)
        {
            int from = data.Modes.IndexOf(mode);
            int to = from + delta;
            if (to < 0 || to >= data.Modes.Count) return;
            data.Modes.Move(from, to);
            Storage.Save(data);
            SelectMode(mode);
        }
        else if (selected is ModeAction action)
        {
            var parent = data.Modes.FirstOrDefault(m => m.Actions.Contains(action));
            if (parent is null) return;
            int from = parent.Actions.IndexOf(action);
            int to = from + delta;
            if (to < 0 || to >= parent.Actions.Count) return;
            parent.Actions.Move(from, to);
            Storage.Save(data);
            SelectAction(parent, action);
        }
    }

    void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    void AddApp_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not Mode mode) return;
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Uygulamalar (*.exe;*.lnk)|*.exe;*.lnk|Tüm dosyalar (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        var action = new ModeAction { Type = ActionType.App, Target = dialog.FileName };
        mode.Actions.Add(action);
        Storage.Save(data);
        SelectAction(mode, action);
    }

    void AddUrl_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not Mode mode) return;
        var action = new ModeAction { Type = ActionType.Url, Target = "https://" };
        mode.Actions.Add(action);
        Storage.Save(data);
        SelectAction(mode, action);
    }

    void AddCloseApp_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not Mode mode) return;
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Uygulamalar (*.exe)|*.exe|Tüm dosyalar (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        var action = new ModeAction { Type = ActionType.CloseApp, Target = dialog.FileName };
        mode.Actions.Add(action);
        Storage.Save(data);
        SelectAction(mode, action);
    }

    void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not ModeAction action) return;
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Uygulamalar (*.exe;*.lnk)|*.exe;*.lnk|Tüm dosyalar (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        action.Target = dialog.FileName;
        Storage.Save(data);
    }

    void BrowseBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not ModeAction action) return;
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Uygulamalar (*.exe)|*.exe" };
        if (dialog.ShowDialog() != true) return;
        action.BrowserPath = dialog.FileName;
        Storage.Save(data);
    }

    void DeleteAction_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not ModeAction action) return;
        var parent = data.Modes.FirstOrDefault(m => m.Actions.Contains(action));
        if (parent is null) return;
        parent.Actions.Remove(action);
        Storage.Save(data);
        SelectMode(parent);
    }

    async void TestAction_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not ModeAction action) return;
        var error = await ModeRunner.TryExecuteAsync(action, data.Settings);
        if (error != null)
            MessageBox.Show(error, "Eylem çalışmadı", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    void Field_LostFocus(object sender, RoutedEventArgs e) => Storage.Save(data);

    async void RunMode_Click(object sender, RoutedEventArgs e)
    {
        if (selected is not Mode mode || mode.Actions.Count == 0) return;

        if (data.Settings.ConfirmBeforeRun)
        {
            var answer = MessageBox.Show($"\"{mode.Name}\" çalıştırılsın mı?", "Modu çalıştır", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;
        }

        var errors = await ModeRunner.RunAsync(mode, data.Settings);
        if (errors.Count > 0)
            MessageBox.Show(string.Join("\n", errors), "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}