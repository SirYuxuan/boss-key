using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BossKey;

public sealed class HotKeyManager : IDisposable
{
    private const int WmHotKey = 0x0312;

    private readonly Window _window;
    private readonly HwndSourceHook _hook;
    private HwndSource? _source;
    private int _currentHotKeyId;

    public HotKeyManager(Window window)
    {
        _window = window;
        _hook = WndProc;
    }

    public event EventHandler? HotKeyPressed;

    public void Initialize()
    {
        _source = (HwndSource?)PresentationSource.FromVisual(_window);
        _source?.AddHook(_hook);
    }

    public bool Register(ModifierKeys modifiers, Key key)
    {
        if (_source?.Handle is not nint handle || handle == nint.Zero)
        {
            return false;
        }

        Unregister();
        _currentHotKeyId = GetHashCode();
        return RegisterHotKey(handle, _currentHotKeyId, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    public void Unregister()
    {
        if (_source?.Handle is nint handle && handle != nint.Zero && _currentHotKeyId != 0)
        {
            _ = UnregisterHotKey(handle, _currentHotKeyId);
            _currentHotKeyId = 0;
        }
    }

    public void Dispose()
    {
        Unregister();
        _source?.RemoveHook(_hook);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == _currentHotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return nint.Zero;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
