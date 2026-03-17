using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace BossKey;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private enum HotKeyCaptureTarget
    {
        None,
        Boss,
        SelfHide
    }

    private readonly Dictionary<nint, WindowScanner.WindowSnapshot> _hiddenWindows = new();
    private readonly BossKeySettings _settings;
    private readonly HotKeyManager _bossHotKeyManager;
    private readonly HotKeyManager _selfHotKeyManager;
    private readonly NotifyIcon _notifyIcon;
    private bool _bossModeActive;
    private bool _selfHiddenByHotKey;
    private HotKeyCaptureTarget _captureTarget;
    private string _statusText = "\u51C6\u5907\u5C31\u7EEA\u3002\u8BF7\u9009\u62E9\u8F6F\u4EF6\u6216\u7A97\u53E3\u6807\u9898\uFF0C\u7136\u540E\u6309\u8001\u677F\u952E\u3002";
    private string _bossModeText = "\u672A\u9690\u85CF";
    private string _bossHotKeyText = string.Empty;
    private string _selfHotKeyText = string.Empty;
    private string _bossHotKeyEditorText = string.Empty;
    private string _selfHotKeyEditorText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _settings = SettingsStore.Load();
        _bossHotKeyManager = new HotKeyManager(this);
        _bossHotKeyManager.HotKeyPressed += (_, _) => Dispatcher.Invoke(ToggleBossMode);
        _selfHotKeyManager = new HotKeyManager(this);
        _selfHotKeyManager.HotKeyPressed += (_, _) => Dispatcher.Invoke(ToggleSelfVisibility);

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "\u8001\u677F\u952E",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMainWindow);

        ProcessItems = new ObservableCollection<ProcessListItem>();
        WindowItems = new ObservableCollection<WindowListItem>();
        SavedProcessItems = new ObservableCollection<BindableSavedProcessItem>(
            _settings.SavedProcesses.Select(item => new BindableSavedProcessItem { ProcessName = item.ProcessName }));
        SavedWindowItems = new ObservableCollection<BindableSavedWindowItem>(
            _settings.SavedWindows.Select(item => new BindableSavedWindowItem { ProcessName = item.ProcessName, Title = item.Title }));

        UpdateHotKeyTexts();
        BossHotKeyEditorText = $"\u5F53\u524D\u503C\uFF1A{FormatHotKey(_settings.Modifiers, _settings.Key)}";
        SelfHotKeyEditorText = $"\u5F53\u524D\u503C\uFF1A{FormatHotKey(_settings.SelfHideModifiers, _settings.SelfHideKey)}";
        RefreshWindowData();

        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
    }

    public ObservableCollection<ProcessListItem> ProcessItems { get; }

    public ObservableCollection<WindowListItem> WindowItems { get; }

    public ObservableCollection<BindableSavedProcessItem> SavedProcessItems { get; }

    public ObservableCollection<BindableSavedWindowItem> SavedWindowItems { get; }

    public string BossHotKeyEditorText
    {
        get => _bossHotKeyEditorText;
        set
        {
            if (_bossHotKeyEditorText == value)
            {
                return;
            }

            _bossHotKeyEditorText = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string BossModeText
    {
        get => _bossModeText;
        set
        {
            if (_bossModeText == value)
            {
                return;
            }

            _bossModeText = value;
            OnPropertyChanged();
        }
    }

    public string SelfHotKeyEditorText
    {
        get => _selfHotKeyEditorText;
        set
        {
            if (_selfHotKeyEditorText == value)
            {
                return;
            }

            _selfHotKeyEditorText = value;
            OnPropertyChanged();
        }
    }

    public string BossHotKeyText
    {
        get => _bossHotKeyText;
        set
        {
            if (_bossHotKeyText == value)
            {
                return;
            }

            _bossHotKeyText = value;
            OnPropertyChanged();
        }
    }

    public string SelfHotKeyText
    {
        get => _selfHotKeyText;
        set
        {
            if (_selfHotKeyText == value)
            {
                return;
            }

            _selfHotKeyText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _bossHotKeyManager.Initialize();
        _selfHotKeyManager.Initialize();
        if (!TryRegisterHotKeys())
        {
            StatusText = "\u5FEB\u6377\u952E\u6CE8\u518C\u5931\u8D25\uFF0C\u8BF7\u66F4\u6362\u7EC4\u5408\u952E\u3002";
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && !_bossModeActive)
        {
            Hide();
            _notifyIcon.Visible = true;
            StatusText = "\u4E3B\u7A97\u53E3\u5DF2\u6700\u5C0F\u5316\u5230\u6258\u76D8\uFF0C\u53CC\u51FB\u6258\u76D8\u56FE\u6807\u53EF\u4EE5\u6062\u590D\u3002";
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _bossHotKeyManager.Dispose();
        _selfHotKeyManager.Dispose();
    }

    private void RefreshProcessesButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshWindowData();
    }

    private void RefreshTitlesButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshWindowData();
    }

    private void SaveBossHotKeyButton_Click(object sender, RoutedEventArgs e)
    {
        PersistSettings();
        UpdateHotKeyTexts();
        BossHotKeyEditorText = $"\u5F53\u524D\u503C\uFF1A{FormatHotKey(_settings.Modifiers, _settings.Key)}";

        if (TryRegisterHotKeys())
        {
            StatusText = $"\u8001\u677F\u952E\u5DF2\u4FDD\u5B58\uFF1A{FormatHotKey(_settings.Modifiers, _settings.Key)}\u3002";
        }
        else
        {
            StatusText = $"\u8001\u677F\u952E\u6CE8\u518C\u5931\u8D25\uFF1A{FormatHotKey(_settings.Modifiers, _settings.Key)}\u3002";
        }
    }

    private void SaveSelfHotKeyButton_Click(object sender, RoutedEventArgs e)
    {
        PersistSettings();
        UpdateHotKeyTexts();
        SelfHotKeyEditorText = $"\u5F53\u524D\u503C\uFF1A{FormatHotKey(_settings.SelfHideModifiers, _settings.SelfHideKey)}";

        if (TryRegisterHotKeys())
        {
            StatusText = $"\u81EA\u8EAB\u5FEB\u6377\u952E\u5DF2\u4FDD\u5B58\uFF1A{FormatHotKey(_settings.SelfHideModifiers, _settings.SelfHideKey)}\u3002";
        }
        else
        {
            StatusText = $"\u81EA\u8EAB\u5FEB\u6377\u952E\u6CE8\u518C\u5931\u8D25\uFF1A{FormatHotKey(_settings.SelfHideModifiers, _settings.SelfHideKey)}\u3002";
        }
    }

    private void RefreshWindowData()
    {
        var windows = WindowScanner.GetVisibleWindows(Process.GetCurrentProcess().Id)
            .OrderBy(window => window.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ProcessItems.Clear();
        foreach (var group in windows
                     .GroupBy(window => window.ProcessName)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            ProcessItems.Add(new ProcessListItem
            {
                ProcessName = group.Key,
                WindowCount = group.Count()
            });
        }

        WindowItems.Clear();
        foreach (var window in windows)
        {
            WindowItems.Add(new WindowListItem
            {
                Handle = window.Handle,
                ProcessName = window.ProcessName,
                Title = window.Title
            });
        }

        StatusText = $"\u5237\u65B0\u5B8C\u6210\uFF0C\u53D1\u73B0 {ProcessItems.Count} \u4E2A\u8F6F\u4EF6\u548C {WindowItems.Count} \u4E2A\u7A97\u53E3\u6807\u9898\u3002";
    }

    private void ToggleBossMode()
    {
        if (_bossModeActive)
        {
            RestoreTargets();
            return;
        }

        var selectedProcessNames = SavedProcessItems.Select(item => item.ProcessName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedTitles = SavedWindowItems.Select(item => item.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedProcessNames.Count == 0 && selectedTitles.Count == 0)
        {
            StatusText = "\u8FD8\u6CA1\u6709\u9009\u62E9\u8981\u9690\u85CF\u7684\u76EE\u6807\u3002";
            return;
        }

        var windows = WindowScanner.GetVisibleWindows(Process.GetCurrentProcess().Id);
        var targets = windows
            .Where(window => selectedProcessNames.Contains(window.ProcessName) || selectedTitles.Contains(window.Title))
            .GroupBy(window => window.Handle)
            .Select(group => group.First())
            .ToList();

        if (targets.Count == 0)
        {
            StatusText = "\u6CA1\u6709\u627E\u5230\u5339\u914D\u7684\u53EF\u89C1\u7A97\u53E3\uFF0C\u8BF7\u5148\u5237\u65B0\u5217\u8868\u3002";
            return;
        }

        _hiddenWindows.Clear();
        foreach (var target in targets)
        {
            _hiddenWindows[target.Handle] = target;
            WindowScanner.HideWindow(target.Handle);
        }

        _bossModeActive = true;
        BossModeText = $"\u5DF2\u9690\u85CF {_hiddenWindows.Count} \u4E2A\u7A97\u53E3";
        StatusText = "\u76EE\u6807\u7A97\u53E3\u5DF2\u9690\u85CF\uFF0C\u672C\u7A0B\u5E8F\u4E3B\u7A97\u53E3\u548C\u6258\u76D8\u56FE\u6807\u4E5F\u5DF2\u6536\u8D77\u3002";

        _notifyIcon.Visible = false;
        Hide();
    }

    private void RestoreTargets()
    {
        foreach (var handle in _hiddenWindows.Keys.ToList())
        {
            WindowScanner.RestoreWindow(handle);
        }

        int restoredCount = _hiddenWindows.Count;
        _hiddenWindows.Clear();
        _bossModeActive = false;
        BossModeText = "\u672A\u9690\u85CF";
        _notifyIcon.Visible = true;
        ShowMainWindow();
        StatusText = $"\u5DF2\u6062\u590D {restoredCount} \u4E2A\u7A97\u53E3\u3002";
    }

    private void ShowMainWindow()
    {
        _notifyIcon.Visible = true;
        _selfHiddenByHotKey = false;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void ToggleSelfVisibility()
    {
        if (_bossModeActive)
        {
            return;
        }

        if (_selfHiddenByHotKey || !IsVisible || WindowState == WindowState.Minimized)
        {
            ShowMainWindow();
            StatusText = "\u672C\u7A0B\u5E8F\u5DF2\u6062\u590D\u663E\u793A\u3002";
            return;
        }

        _selfHiddenByHotKey = true;
        _notifyIcon.Visible = false;
        Hide();
    }

    private void UpdateHotKeyTexts()
    {
        BossHotKeyText = $"\u5F53\u524D\u8001\u677F\u952E\uFF1A{FormatHotKey(_settings.Modifiers, _settings.Key)}";
        SelfHotKeyText = $"\u5F53\u524D\u81EA\u8EAB\u5FEB\u6377\u952E\uFF1A{FormatHotKey(_settings.SelfHideModifiers, _settings.SelfHideKey)}";
    }

    private void BossHotKeyCaptureBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _captureTarget = HotKeyCaptureTarget.Boss;
        UnregisterHotKeys();
        BossHotKeyEditorText = "\u8BF7\u76F4\u63A5\u6309\u4E0B\u65B0\u7684\u5FEB\u6377\u952E";
    }

    private void BossHotKeyCaptureBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_captureTarget == HotKeyCaptureTarget.Boss)
        {
            _captureTarget = HotKeyCaptureTarget.None;
            BossHotKeyEditorText = $"\u5F53\u524D\u503C\uFF1A{FormatHotKey(_settings.Modifiers, _settings.Key)}";
            _ = TryRegisterHotKeys();
        }
    }

    private void SelfHotKeyCaptureBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _captureTarget = HotKeyCaptureTarget.SelfHide;
        UnregisterHotKeys();
        SelfHotKeyEditorText = "\u8BF7\u76F4\u63A5\u6309\u4E0B\u65B0\u7684\u5FEB\u6377\u952E";
    }

    private void SelfHotKeyCaptureBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_captureTarget == HotKeyCaptureTarget.SelfHide)
        {
            _captureTarget = HotKeyCaptureTarget.None;
            SelfHotKeyEditorText = $"\u5F53\u524D\u503C\uFF1A{FormatHotKey(_settings.SelfHideModifiers, _settings.SelfHideKey)}";
            _ = TryRegisterHotKeys();
        }
    }

    private void BossHotKeyCaptureBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_captureTarget != HotKeyCaptureTarget.Boss)
        {
            return;
        }

        CaptureHotKey(e, HotKeyCaptureTarget.Boss);
    }

    private void SelfHotKeyCaptureBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_captureTarget != HotKeyCaptureTarget.SelfHide)
        {
            return;
        }

        CaptureHotKey(e, HotKeyCaptureTarget.SelfHide);
    }

    private void CaptureHotKey(System.Windows.Input.KeyEventArgs e, HotKeyCaptureTarget target)
    {
        e.Handled = true;

        Key actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
        ModifierKeys modifiers = Keyboard.Modifiers;

        if (actualKey is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            SetEditorText(target, "\u8BF7\u518D\u6309\u4E00\u4E2A\u666E\u901A\u6309\u952E\uFF0C\u53EF\u4EE5\u5E26\u6216\u4E0D\u5E26\u4FEE\u9970\u952E");
            StatusText = "\u5FEB\u6377\u952E\u9700\u8981\u81F3\u5C11\u5305\u542B\u4E00\u4E2A\u666E\u901A\u6309\u952E\u3002";
            return;
        }

        if (target == HotKeyCaptureTarget.Boss)
        {
            _settings.Modifiers = modifiers;
            _settings.Key = actualKey;
            BossHotKeyEditorText = $"\u5F85\u4FDD\u5B58\uFF1A{FormatHotKey(modifiers, actualKey)}";
            StatusText = $"\u5DF2\u6355\u83B7\u8001\u677F\u952E\uFF1A{FormatHotKey(modifiers, actualKey)}\uFF0C\u70B9\u51FB\u201C\u4FDD\u5B58\u8001\u677F\u952E\u201D\u751F\u6548\u3002";
            return;
        }

        _settings.SelfHideModifiers = modifiers;
        _settings.SelfHideKey = actualKey;
        SelfHotKeyEditorText = $"\u5F85\u4FDD\u5B58\uFF1A{FormatHotKey(modifiers, actualKey)}";
        StatusText = $"\u5DF2\u6355\u83B7\u81EA\u8EAB\u5FEB\u6377\u952E\uFF1A{FormatHotKey(modifiers, actualKey)}\uFF0C\u70B9\u51FB\u201C\u4FDD\u5B58\u81EA\u8EAB\u5FEB\u6377\u952E\u201D\u751F\u6548\u3002";
    }

    private void ProcessSourceGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.DataGrid { SelectedItem: ProcessListItem item })
        {
            return;
        }

        if (SavedProcessItems.Any(saved => string.Equals(saved.ProcessName, item.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = $"\u8F6F\u4EF6 {item.ProcessName} \u5DF2\u5728\u4FDD\u5B58\u5217\u8868\u4E2D\u3002";
            return;
        }

        SavedProcessItems.Add(new BindableSavedProcessItem { ProcessName = item.ProcessName });
        PersistSettings();
        StatusText = $"\u5DF2\u6DFB\u52A0\u8F6F\u4EF6\uFF1A{item.ProcessName}\u3002";
    }

    private void SavedProcessGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.DataGrid { SelectedItem: BindableSavedProcessItem item })
        {
            return;
        }

        SavedProcessItems.Remove(item);
        PersistSettings();
        StatusText = $"\u5DF2\u79FB\u9664\u8F6F\u4EF6\uFF1A{item.ProcessName}\u3002";
    }

    private void WindowSourceGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.DataGrid { SelectedItem: WindowListItem item })
        {
            return;
        }

        if (SavedWindowItems.Any(saved =>
                string.Equals(saved.Title, item.Title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(saved.ProcessName, item.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = $"\u7A97\u53E3 {item.Title} \u5DF2\u5728\u4FDD\u5B58\u5217\u8868\u4E2D\u3002";
            return;
        }

        SavedWindowItems.Add(new BindableSavedWindowItem
        {
            ProcessName = item.ProcessName,
            Title = item.Title
        });
        PersistSettings();
        StatusText = $"\u5DF2\u6DFB\u52A0\u7A97\u53E3\u6807\u9898\uFF1A{item.Title}\u3002";
    }

    private void SavedWindowGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.DataGrid { SelectedItem: BindableSavedWindowItem item })
        {
            return;
        }

        SavedWindowItems.Remove(item);
        PersistSettings();
        StatusText = $"\u5DF2\u79FB\u9664\u7A97\u53E3\u6807\u9898\uFF1A{item.Title}\u3002";
    }

    private void PersistSettings()
    {
        _settings.SavedProcesses = SavedProcessItems
            .Select(item => new SavedProcessItem { ProcessName = item.ProcessName })
            .ToList();

        _settings.SavedWindows = SavedWindowItems
            .Select(item => new SavedWindowItem { ProcessName = item.ProcessName, Title = item.Title })
            .ToList();

        SettingsStore.Save(_settings);
    }

    private bool TryRegisterHotKeys()
    {
        if (_captureTarget != HotKeyCaptureTarget.None)
        {
            return true;
        }

        bool bossRegistered = _bossHotKeyManager.Register(_settings.Modifiers, _settings.Key);
        bool selfRegistered = _selfHotKeyManager.Register(_settings.SelfHideModifiers, _settings.SelfHideKey);
        return bossRegistered && selfRegistered;
    }

    private void UnregisterHotKeys()
    {
        _bossHotKeyManager.Unregister();
        _selfHotKeyManager.Unregister();
    }

    private void SetEditorText(HotKeyCaptureTarget target, string text)
    {
        if (target == HotKeyCaptureTarget.Boss)
        {
            BossHotKeyEditorText = text;
            return;
        }

        SelfHotKeyEditorText = text;
    }

    private static string FormatHotKey(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        if (key != Key.None)
        {
            parts.Add(key.ToString());
        }

        return parts.Count == 0 ? "\u672A\u8BBE\u7F6E" : string.Join(" + ", parts);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
