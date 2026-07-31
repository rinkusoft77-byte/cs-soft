using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VisionAssist.Overlay;

/// <summary>
/// The Win32 calls the overlay needs. Kept in one place so the P/Invoke
/// signatures can be checked against each other.
/// </summary>
internal static class Win32
{
    // ------------------------------------------------------- window styles

    internal const int GWL_EXSTYLE = -20;

    /// <summary>Mouse input falls through to whatever is underneath.</summary>
    internal const int WS_EX_TRANSPARENT = 0x00000020;

    /// <summary>Clicking the window does not take focus away from the game.</summary>
    internal const int WS_EX_NOACTIVATE = 0x08000000;

    /// <summary>Keeps the window out of the taskbar and out of Alt+Tab.</summary>
    internal const int WS_EX_TOOLWINDOW = 0x00000080;

    internal const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    /// <summary>
    /// GetWindowLongPtrW only exists on 64-bit Windows; on 32-bit the Ptr name
    /// is a macro for the plain one, so the right export has to be picked here.
    /// </summary>
    internal static int GetWindowExStyle(IntPtr hWnd)
        => IntPtr.Size == 8
            ? (int)GetWindowLongPtr64(hWnd, GWL_EXSTYLE).ToInt64()
            : GetWindowLong32(hWnd, GWL_EXSTYLE);

    internal static void SetWindowExStyle(IntPtr hWnd, int style)
    {
        if (IntPtr.Size == 8) SetWindowLongPtr64(hWnd, GWL_EXSTYLE, new IntPtr(style));
        else SetWindowLong32(hWnd, GWL_EXSTYLE, style);
    }

    // ---------------------------------------------------------- z-order

    internal static readonly IntPtr HWND_TOPMOST = new(-1);

    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// Re-asserts topmost. A game going fullscreen pushes everything else down
    /// the z-order, so this is called on a timer rather than just once.
    /// </summary>
    internal static void PushToTop(IntPtr hWnd)
        => SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    // ------------------------------------------------------ foreground app

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Name of the process that currently owns the foreground window, lowercase
    /// and without the extension. Empty when it cannot be determined - which is
    /// common and not worth reporting, since access to another process is denied
    /// often enough on a normal desktop.
    /// </summary>
    internal static string ForegroundProcessName()
    {
        IntPtr window = GetForegroundWindow();
        if (window == IntPtr.Zero) return string.Empty;

        GetWindowThreadProcessId(window, out uint pid);
        if (pid == 0) return string.Empty;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName.ToLowerInvariant();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                                      or System.ComponentModel.Win32Exception)
        {
            return string.Empty;
        }
    }

    // ------------------------------------------------------ keyboard hook

    internal const int WH_KEYBOARD_LL = 13;
    internal const int HC_ACTION = 0;

    internal const int WM_KEYDOWN = 0x0100;
    internal const int WM_KEYUP = 0x0101;
    internal const int WM_SYSKEYDOWN = 0x0104;
    internal const int WM_SYSKEYUP = 0x0105;

    internal const int VK_MENU = 0x12;   // either Alt
    internal const int VK_LMENU = 0xA4;
    internal const int VK_RMENU = 0xA5;
    internal const int VK_TAB = 0x09;

    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern IntPtr SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern IntPtr GetModuleHandleW(string? lpModuleName);
}
