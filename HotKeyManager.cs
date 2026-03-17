using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BossKey;

public sealed class HotKeyManager : IDisposable
{
    private const int WmHotKey = 0x0312;
    private static int _nextHotKeyId;

    private readonly Window _window;
    private readonly HwndSourceHook _hook;
    private readonly int _hotKeyId;
    private HwndSource? _source;

    public HotKeyManager(Window window)
    {
        _window = window;
        _hook = WndProc;
        _hotKeyId = Interlocked.Increment(ref _nextHotKeyId);
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
        return RegisterHotKey(handle, _hotKeyId, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    public void Unregister()
    {
        if (_source?.Handle is nint handle && handle != nint.Zero)
        {
            _ = UnregisterHotKey(handle, _hotKeyId);
        }
    }

    public void Dispose()
    {
        Unregister();
        _source?.RemoveHook(_hook);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == _hotKeyId)
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
