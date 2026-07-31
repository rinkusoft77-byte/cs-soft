namespace VisionAssist.Overlay;

/// <summary>
/// Watches for a <b>tap</b> of Alt anywhere on the desktop and raises
/// <see cref="Tapped"/>.
///
/// A tap, not a press: Alt is half of Alt+Tab and Alt+F4, and CS2 players bind
/// things to it, so the menu must not open every time Alt is held as a modifier.
/// The rule is that Alt goes down and comes back up with no other key touched in
/// between, and quickly enough that it was clearly a deliberate tap.
///
/// The hook is passive - it never swallows a key, so Alt keeps working normally
/// in the game and everywhere else.
/// </summary>
internal sealed class AltTapHook : IDisposable
{
    private static readonly TimeSpan MaxTapDuration = TimeSpan.FromMilliseconds(400);

    private readonly Win32.LowLevelKeyboardProc _callback;
    private IntPtr _hook = IntPtr.Zero;

    private bool _altDown;
    private bool _otherKeyDuringAlt;
    private DateTime _altDownAt;

    /// <summary>Raised on the hook thread; marshal to the UI thread before touching a form.</summary>
    public event Action? Tapped;

    public AltTapHook()
    {
        // The delegate has to be held in a field: if it is collected while the
        // hook is installed, the next keystroke crashes the process.
        _callback = HookProc;
    }

    public bool Install()
    {
        if (_hook != IntPtr.Zero) return true;

        // A low-level hook is global and needs no module handle of its own, but
        // passing the current one is the documented form.
        IntPtr module = Win32.GetModuleHandleW(null);
        _hook = Win32.SetWindowsHookExW(Win32.WH_KEYBOARD_LL, _callback, module, 0);
        return _hook != IntPtr.Zero;
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == Win32.HC_ACTION)
        {
            var data = System.Runtime.InteropServices.Marshal
                .PtrToStructure<Win32.KBDLLHOOKSTRUCT>(lParam);

            int message = wParam.ToInt32();
            bool isDown = message is Win32.WM_KEYDOWN or Win32.WM_SYSKEYDOWN;
            bool isUp = message is Win32.WM_KEYUP or Win32.WM_SYSKEYUP;
            bool isAlt = data.vkCode is Win32.VK_MENU or Win32.VK_LMENU or Win32.VK_RMENU;

            if (isAlt && isDown)
            {
                // Auto-repeat while held must not reset the timer.
                if (!_altDown)
                {
                    _altDown = true;
                    _otherKeyDuringAlt = false;
                    _altDownAt = DateTime.UtcNow;
                }
            }
            else if (isAlt && isUp)
            {
                bool wasTap = _altDown
                              && !_otherKeyDuringAlt
                              && DateTime.UtcNow - _altDownAt <= MaxTapDuration;

                _altDown = false;
                _otherKeyDuringAlt = false;

                if (wasTap) Tapped?.Invoke();
            }
            else if (isDown && _altDown)
            {
                // Alt+something: a modifier, not a tap.
                _otherKeyDuringAlt = true;
            }
        }

        return Win32.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero) return;
        Win32.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }
}
