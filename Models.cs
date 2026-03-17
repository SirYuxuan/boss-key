using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BossKey;

public sealed class ProcessListItem
{
    public string ProcessName { get; init; } = string.Empty;

    public int WindowCount { get; init; }
}

public sealed class WindowListItem
{
    public nint Handle { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ProcessName { get; init; } = string.Empty;
}

public sealed class SavedProcessItem
{
    public string ProcessName { get; set; } = string.Empty;
}

public sealed class SavedWindowItem
{
    public string Title { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;
}

public sealed class BossKeySettings
{
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;

    public Key Key { get; set; } = Key.B;

    public ModifierKeys SelfHideModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;

    public Key SelfHideKey { get; set; } = Key.H;

    public List<SavedProcessItem> SavedProcesses { get; set; } = [];

    public List<SavedWindowItem> SavedWindows { get; set; } = [];
}

public sealed class BindableSavedProcessItem : INotifyPropertyChanged
{
    private string _processName = string.Empty;

    public string ProcessName
    {
        get => _processName;
        set
        {
            if (_processName == value)
            {
                return;
            }

            _processName = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class BindableSavedWindowItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _processName = string.Empty;

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            OnPropertyChanged();
        }
    }

    public string ProcessName
    {
        get => _processName;
        set
        {
            if (_processName == value)
            {
                return;
            }

            _processName = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
