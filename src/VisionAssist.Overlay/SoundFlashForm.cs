using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VisionAssist.Overlay;

/// <summary>
/// Screen-edge flashes for a player who cannot hear the game.
///
/// The small panel in the HUD tells you where a sound was, but only if you are
/// looking at it, and while playing you are looking at the crosshair. This covers
/// the whole screen instead and lights up the edge the sound came from, so it
/// registers in peripheral vision without anyone having to glance anywhere.
///
/// Three zones, because a stereo signal supports exactly three honest answers:
/// left, right, and "somewhere ahead or behind" - front and back cannot be
/// separated, so the middle zone claims nothing about which.
///
/// Everything here comes from sound the game already played out loud.
/// </summary>
internal sealed class SoundFlashForm : Form
{
    private static readonly Color KeyColor = Color.FromArgb(0xFF, 0x00, 0xFF);

    /// <summary>Below this a zone is treated as dark, and an idle form hides itself.</summary>
    private const double VisibleFloor = 0.02;

    /// <summary>Fraction of brightness kept per frame. ~0.86 fades in about 400 ms.</summary>
    private const double Decay = 0.86;

    private readonly OverlaySettings _settings;
    private readonly SoundMeter _sound;

    private double _left, _centre, _right;
    private bool _leftLoud, _centreLoud, _rightLoud;
    private long _lastEventSeen;

    public SoundFlashForm(OverlaySettings settings, SoundMeter sound)
    {
        _settings = settings;
        _sound = sound;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = KeyColor;
        TransparencyKey = KeyColor;
        TopMost = true;
        DoubleBuffered = true;
        Text = "VisionAssist Sound";
        Visible = false;

        // Full bounds rather than the working area: the game covers the taskbar,
        // so the flash has to as well.
        var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        Bounds = screen;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Click-through always: unlike the HUD this window never has anything
            // to click, and it covers the entire screen.
            cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_NOACTIVATE
                          | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TRANSPARENT;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    /// <summary>
    /// Folds any new transients into the three zones and fades the old ones.
    /// Driven from the overlay's timer so there is only one clock in the program.
    /// </summary>
    public void Tick()
    {
        if (!_settings.EdgeFlashEnabled || !_sound.Running)
        {
            if (Visible) Visible = false;
            _left = _centre = _right = 0;
            return;
        }

        foreach (var evt in _sound.RecentEvents())
        {
            if (evt.At <= _lastEventSeen) continue;
            _lastEventSeen = evt.At;

            // Ratio maps to brightness: a heavy thump should read brighter than a
            // distant scuff, but everything above ~8x is simply "loud".
            double strength = Math.Clamp(Math.Log2(Math.Max(1, evt.Ratio)) / 3.0, 0.25, 1.0);

            if (evt.Pan < -0.33)
            {
                _left = Math.Max(_left, strength);
                _leftLoud = evt.Loud;
            }
            else if (evt.Pan > 0.33)
            {
                _right = Math.Max(_right, strength);
                _rightLoud = evt.Loud;
            }
            else
            {
                _centre = Math.Max(_centre, strength);
                _centreLoud = evt.Loud;
            }
        }

        _left *= Decay;
        _centre *= Decay;
        _right *= Decay;

        bool anything = _left > VisibleFloor || _centre > VisibleFloor || _right > VisibleFloor;

        if (anything)
        {
            if (!Visible) Visible = true;
            Win32.PushToTop(Handle);
            Invalidate();
        }
        else if (Visible)
        {
            // Hidden rather than painted empty: an invisible window costs nothing,
            // and a full-screen transparent one being composited does.
            Visible = false;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;

        var palette = Palette.Find(_settings.Theme);
        int thickness = Math.Max(12, (int)(_settings.Unit * 2.2));

        if (_left > VisibleFloor)
        {
            DrawEdge(g, new Rectangle(0, 0, thickness, Height),
                _left, _leftLoud, palette, LinearGradientMode.Horizontal, false);
        }

        if (_right > VisibleFloor)
        {
            DrawEdge(g, new Rectangle(Width - thickness, 0, thickness, Height),
                _right, _rightLoud, palette, LinearGradientMode.Horizontal, true);
        }

        if (_centre > VisibleFloor)
        {
            // Along the top, and only a third of the width, so it never looks like
            // a claim about distance or side.
            int width = Width / 3;
            DrawEdge(g, new Rectangle((Width - width) / 2, 0, width, thickness),
                _centre, _centreLoud, palette, LinearGradientMode.Vertical, true);
        }
    }

    /// <summary>
    /// One edge bar, brightest at the screen edge and fading inwards so it reads
    /// as a glow rather than a solid block sitting on top of the game.
    /// </summary>
    private static void DrawEdge(Graphics g, Rectangle rect, double intensity, bool loud,
        Palette palette, LinearGradientMode direction, bool reverse)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;

        int alpha = (int)Math.Clamp(intensity * 235, 0, 235);
        Color bright = Color.FromArgb(alpha, loud ? Palette.Danger : palette.TeamCt);
        Color faded = Color.FromArgb(0, bright);

        using var brush = new LinearGradientBrush(rect,
            reverse ? faded : bright,
            reverse ? bright : faded,
            direction);

        g.FillRectangle(brush, rect);
    }
}
