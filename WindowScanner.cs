using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BossKey;

public static class WindowScanner
{
    public static IReadOnlyList<WindowSnapshot> GetVisibleWindows(int ignoredProcessId)
    {
        return GetWindows(ignoredProcessId, visibleOnly: true);
    }

    public static IReadOnlyList<WindowSnapshot> GetAllWindows(int ignoredProcessId)
    {
        return GetWindows(ignoredProcessId, visibleOnly: false);
    }

    public static bool IsVisible(nint handle)
    {
        return handle != nint.Zero && IsWindowVisible(handle);
    }

    private static IReadOnlyList<WindowSnapshot> GetWindows(int ignoredProcessId, bool visibleOnly)
    {
        var snapshots = new List<WindowSnapshot>();

        EnumWindows((handle, lParam) =>
        {
            if (handle == nint.Zero)
            {
                return true;
            }

            bool isVisible = IsWindowVisible(handle);
            if (visibleOnly && !isVisible)
            {
                return true;
            }

            if (GetWindowTextLength(handle) == 0)
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var processId);
            if (processId == 0 || processId == (uint)ignoredProcessId)
            {
                return true;
            }

            string title = GetWindowTitle(handle);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            try
            {
                using var process = Process.GetProcessById((int)processId);
                snapshots.Add(new WindowSnapshot(handle, process.ProcessName, title));
            }
            catch
            {
            }

            return true;
        }, nint.Zero);

        return snapshots;
    }

    public static void HideWindow(nint handle)
    {
        _ = ShowWindow(handle, ShowWindowCommand.Hide);
    }

    public static void RestoreWindow(nint handle)
    {
        _ = ShowWindow(handle, ShowWindowCommand.Restore);
    }

    private static string GetWindowTitle(nint handle)
    {
        int length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString().Trim();
    }

    public sealed record WindowSnapshot(nint Handle, string ProcessName, string Title);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, ShowWindowCommand nCmdShow);

    private enum ShowWindowCommand
    {
        Hide = 0,
        Restore = 9
    }
}
