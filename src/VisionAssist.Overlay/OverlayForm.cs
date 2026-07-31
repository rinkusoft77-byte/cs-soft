using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using VisionAssist.Companion;

namespace VisionAssist.Overlay;

/// <summary>
/// The HUD itself: a borderless, always-on-top, click-through window drawn over
/// the game.
///
/// Click-through and no-activate together are what make it usable while playing -
/// the window is never focused, never steals a click, and never appears in
/// Alt+Tab. Everything painted in <see cref="Form.TransparencyKey"/> is a hole,
/// so the HUD is a few panels floating over the game rather than a grey box.
///
/// It cannot draw over exclusive fullscreen; nothing can without hooking the
/// game's renderer, which this program does not do. Fullscreen Windowed works.
/// </summary>
internal sealed class OverlayForm : Form
{
    /// <summary>Painted where the window should be a hole. Nothing else may use it.</summary>
    private static readonly Color KeyColor = Color.FromArgb(0xFF, 0x00, 0xFF);

    private readonly OverlaySettings _settings;
    private readonly GsiHost _gsi;
    private readonly SoundMeter _sound;
    private readonly AltTapHook _hook = new();
    private readonly System.Windows.Forms.Timer _timer;

    private Palette _palette;
    private Font _labelFont = null!;
    private Font _valueFont = null!;
    private Font _noteFont = null!;
    private Font _alertFont = null!;

    /// <summary>Unit the current fonts were built for, so they are not rebuilt every tick.</summary>
    private int _fontUnit = -1;

    private SettingsForm? _settingsForm;
    private bool _clickThrough = true;

    public OverlayForm(OverlaySettings settings, GsiHost gsi, SoundMeter sound)
    {
        _settings = settings;
        _gsi = gsi;
        _sound = sound;
        _palette = Palette.Find(settings.Theme);

        Strings.Language = settings.Language;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = KeyColor;
        TransparencyKey = KeyColor;
        TopMost = true;
        Text = "VisionAssist Overlay";
        DoubleBuffered = true;

        BuildFonts();
        ApplyGeometry();

        _timer = new System.Windows.Forms.Timer { Interval = 100 };
        _timer.Tick += OnTick;

        // The hook fires on its own thread; hop to the UI thread before touching
        // a window.
        _hook.Tapped += () =>
        {
            if (!IsDisposed && IsHandleCreated) BeginInvoke(ToggleSettings);
        };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TOOLWINDOW;
            if (_clickThrough) cp.ExStyle |= Win32.WS_EX_TRANSPARENT;
            return cp;
        }
    }

    /// <summary>Never take focus, whatever happens - this is a game overlay.</summary>
    protected override bool ShowWithoutActivation => true;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyClickThrough(true);
        Win32.PushToTop(Handle);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();

        if (!_hook.Install())
        {
            // Not fatal: the HUD still works, only the Alt shortcut is missing,
            // and the tray icon can still open the menu.
            TrayNotify("Alt tugmasi ishlamaydi (hook o'rnatilmadi). Menyu tray orqali ochiladi.");
        }

        if (_settings.SoundEnabled) StartSound();
    }

    // ------------------------------------------------------------- appearance

    private void BuildFonts()
    {
        int unit = _settings.Unit;
        if (unit == _fontUnit) return;
        _fontUnit = unit;

        _labelFont?.Dispose();
        _valueFont?.Dispose();
        _noteFont?.Dispose();
        _alertFont?.Dispose();

        _labelFont = new Font("Segoe UI", unit * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel);
        _valueFont = new Font("Segoe UI", unit * 2.1f, FontStyle.Bold, GraphicsUnit.Pixel);
        _noteFont = new Font("Segoe UI", unit * 0.6f, FontStyle.Regular, GraphicsUnit.Pixel);
        _alertFont = new Font("Segoe UI", unit * 1.15f, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    /// <summary>Re-reads size, corner and opacity from the settings.</summary>
    public void ApplyGeometry()
    {
        _palette = Palette.Find(_settings.Theme);
        Strings.Language = _settings.Language;
        BuildFonts();

        var layout = ComputeLayout();
        Size = new Size(layout.Width, layout.Height);
        Opacity = Math.Clamp(_settings.Opacity, 0.35, 1.0);

        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        int margin = _settings.Unit;

        Location = _settings.Corner switch
        {
            OverlayCorner.TopRight => new Point(screen.Right - Width - margin, screen.Top + margin),
            OverlayCorner.BottomLeft => new Point(screen.Left + margin, screen.Bottom - Height - margin),
            OverlayCorner.BottomRight => new Point(screen.Right - Width - margin, screen.Bottom - Height - margin),
            _ => new Point(screen.Left + margin, screen.Top + margin),
        };

        Invalidate();
    }

    private void ApplyClickThrough(bool enabled)
    {
        _clickThrough = enabled;
        if (!IsHandleCreated) return;

        int style = Win32.GetWindowExStyle(Handle);
        style = enabled
            ? style | Win32.WS_EX_TRANSPARENT
            : style & ~Win32.WS_EX_TRANSPARENT;
        Win32.SetWindowExStyle(Handle, style);
    }

    // ------------------------------------------------------------------ layout

    /// <summary>
    /// Every rectangle the HUD draws into. Computed before painting so the window
    /// can be resized to exactly the content - anything left over would be a
    /// transparent hole, but the corner anchoring would be wrong.
    /// </summary>
    private readonly struct HudLayout
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public Rectangle Header { get; init; }
        public Rectangle[] Alerts { get; init; }
        public Rectangle Defuse { get; init; }
        public Rectangle[] Cards { get; init; }
        public Rectangle Chips { get; init; }
        public Rectangle Sound { get; init; }
        public bool HasDefuse { get; init; }
        public bool HasSound { get; init; }
    }

    private HudLayout ComputeLayout()
    {
        int unit = _settings.Unit;
        int gap = (int)(unit * 0.45);
        int width = unit * 21;

        var snapshot = _gsi.Current;
        int alertCount = CountAlerts(snapshot);
        bool hasDefuse = DefuseState(snapshot, out _, out _);
        bool hasSound = _settings.SoundEnabled;

        int headerHeight = (int)(unit * 1.8);
        int alertHeight = (int)(unit * 1.9);
        int cardHeight = (int)(unit * 3.5);
        int chipsHeight = (int)(unit * 1.5);
        int soundHeight = (int)(unit * 4.6);

        int y = 0;
        var header = new Rectangle(0, y, width, headerHeight);
        y += headerHeight + gap;

        var alerts = new Rectangle[alertCount];
        for (int i = 0; i < alertCount; i++)
        {
            alerts[i] = new Rectangle(0, y, width, alertHeight);
            y += alertHeight + gap;
        }

        var defuse = Rectangle.Empty;
        if (hasDefuse)
        {
            defuse = new Rectangle(0, y, width, alertHeight);
            y += alertHeight + gap;
        }

        // Two by two: four numbers big enough to read without looking for them.
        int cardWidth = (width - gap) / 2;
        var cards = new[]
        {
            new Rectangle(0, y, cardWidth, cardHeight),
            new Rectangle(cardWidth + gap, y, width - cardWidth - gap, cardHeight),
            new Rectangle(0, y + cardHeight + gap, cardWidth, cardHeight),
            new Rectangle(cardWidth + gap, y + cardHeight + gap, width - cardWidth - gap, cardHeight),
        };
        y += cardHeight * 2 + gap * 2;

        var chips = new Rectangle(0, y, width, chipsHeight);
        y += chipsHeight + gap;

        var sound = Rectangle.Empty;
        if (hasSound)
        {
            sound = new Rectangle(0, y, width, soundHeight);
            y += soundHeight;
        }
        else
        {
            y -= gap;
        }

        return new HudLayout
        {
            Width = width,
            Height = Math.Max(unit * 4, y),
            Header = header,
            Alerts = alerts,
            Defuse = defuse,
            Cards = cards,
            Chips = chips,
            Sound = sound,
            HasDefuse = hasDefuse,
            HasSound = hasSound,
        };
    }

    private static int CountAlerts(OverlaySnapshot s)
    {
        if (!s.Connected) return 1;
        if (!s.InGame) return 0;

        int count = 0;
        if (s.BombState is "planted" or "defused" or "exploded") count++;
        if (s.Burning > 0) count++;
        if (s.Flashed > 60) count++;
        if (s.Smoked > 60) count++;
        return count;
    }

    /// <summary>
    /// Whether the defuse readout applies, and if so how much time is spare.
    /// Bare-handed is 10 s, with a kit 5 s.
    /// </summary>
    private bool DefuseState(OverlaySnapshot s, out double spare, out bool possible)
    {
        spare = 0;
        possible = false;

        if (!s.Connected || s.BombState != "planted" || s.CountdownPhase != "bomb") return false;
        if (!string.Equals(s.Team, "CT", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.CountdownSeconds is null) return false;

        double remaining = Math.Max(0, s.CountdownSeconds.Value - _gsi.MillisecondsSinceUpdate / 1000.0);
        spare = remaining - (s.DefuseKit ? 5.0 : 10.0);
        possible = spare >= 0;
        return true;
    }

    // ------------------------------------------------------------------- paint

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        var snapshot = _gsi.Current;
        var layout = ComputeLayout();
        Color accent = _palette.Accent(snapshot.Team);

        DrawHeader(g, layout.Header, snapshot, accent);

        int index = 0;
        if (!snapshot.Connected)
        {
            if (layout.Alerts.Length > 0)
                DrawBar(g, layout.Alerts[0], Strings.Get("gsiMissing"), Palette.Edge, Palette.Muted);
        }
        else
        {
            if (snapshot.BombState == "planted" && index < layout.Alerts.Length)
                DrawBar(g, layout.Alerts[index++], Strings.Get("bombPlanted"), Palette.Danger, Color.White);
            if (snapshot.BombState == "defused" && index < layout.Alerts.Length)
                DrawBar(g, layout.Alerts[index++], Strings.Get("defused"), Palette.Ok, Color.Black);
            if (snapshot.BombState == "exploded" && index < layout.Alerts.Length)
                DrawBar(g, layout.Alerts[index++], Strings.Get("exploded"), Palette.Danger, Color.White);
            if (snapshot.Burning > 0 && index < layout.Alerts.Length)
                DrawBar(g, layout.Alerts[index++], Strings.Get("burning"), Color.FromArgb(0xFF, 0x6A, 0x00), Color.Black);
            if (snapshot.Flashed > 60 && index < layout.Alerts.Length)
                DrawBar(g, layout.Alerts[index++], Strings.Get("blind"), Color.White, Color.Black);
            if (snapshot.Smoked > 60 && index < layout.Alerts.Length)
                DrawBar(g, layout.Alerts[index], Strings.Get("smoked"), Palette.Edge, Palette.Foreground);
        }

        if (layout.HasDefuse && DefuseState(snapshot, out double spare, out bool possible))
        {
            string text = possible
                ? $"{Strings.Get("defuseYes")}  +{spare:0.0}s"
                : Strings.Get("defuseNo");
            DrawBar(g, layout.Defuse, text,
                possible ? Palette.Ok : Palette.Danger,
                possible ? Color.Black : Color.White);
        }

        DrawHealthCard(g, layout.Cards[0], snapshot);
        DrawArmorCard(g, layout.Cards[1], snapshot, accent);
        DrawAmmoCard(g, layout.Cards[2], snapshot, accent);
        DrawTimerCard(g, layout.Cards[3], snapshot, accent);

        DrawChips(g, layout.Chips, snapshot, accent);

        if (layout.HasSound) DrawSound(g, layout.Sound, accent);
    }

    private void DrawHeader(Graphics g, Rectangle rect, OverlaySnapshot s, Color accent)
    {
        DrawPanel(g, rect, Palette.Edge);

        int pad = (int)(_settings.Unit * 0.5);
        var inner = new Rectangle(rect.X + pad, rect.Y, rect.Width - pad * 2, rect.Height);

        string status = !s.Connected
            ? (_gsi.MillisecondsSinceUpdate > 0 ? Strings.Get("noGame") : Strings.Get("waiting"))
            : s.InGame ? (s.MapName ?? string.Empty).ToUpperInvariant() : Strings.Get("menu");

        Color statusColor = s.Connected ? (s.InGame ? accent : Palette.Warn) : Palette.Muted;

        using var statusBrush = new SolidBrush(statusColor);
        using var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        g.DrawString(status, _labelFont, statusBrush, inner, format);

        // Score on the right, each side in its own team colour.
        if (s.Connected)
        {
            string score = $"{s.ScoreCt} : {s.ScoreT}";
            SizeF size = g.MeasureString(score, _labelFont);
            using var scoreBrush = new SolidBrush(Palette.Foreground);
            g.DrawString(score, _labelFont, scoreBrush,
                new RectangleF(inner.Right - size.Width, inner.Y, size.Width, inner.Height), format);
        }
    }

    private void DrawHealthCard(Graphics g, Rectangle rect, OverlaySnapshot s)
    {
        Color color = !s.InGame ? Palette.Muted
            : s.Health <= 0 ? Palette.Muted
            : s.Health <= _settings.LowHealthThreshold ? Palette.Danger
            : s.Health < 100 ? Palette.Warn
            : Palette.Ok;

        DrawCard(g, rect, Strings.Get("health"),
            s.InGame ? s.Health.ToString() : "–", string.Empty, color, color);
    }

    private void DrawArmorCard(Graphics g, Rectangle rect, OverlaySnapshot s, Color accent)
    {
        DrawCard(g, rect, Strings.Get("armor"),
            s.InGame ? s.Armor.ToString() : "–",
            s.Helmet ? Strings.Get("helmet") : string.Empty,
            accent, Palette.Foreground);
    }

    private void DrawAmmoCard(Graphics g, Rectangle rect, OverlaySnapshot s, Color accent)
    {
        string label = string.IsNullOrEmpty(s.WeaponName) ? Strings.Get("weapon") : s.WeaponName!;
        string value = s.AmmoClip?.ToString() ?? (s.InGame ? Strings.Get("noWeapon") : "–");
        string note = s.AmmoReserve is int reserve ? $"+ {reserve}" : string.Empty;
        Color valueColor = s.AmmoClip == 0 ? Palette.Danger : Palette.Foreground;

        DrawCard(g, rect, label, value, note, s.Reloading ? Palette.Warn : accent, valueColor);
    }

    private void DrawTimerCard(Graphics g, Rectangle rect, OverlaySnapshot s, Color accent)
    {
        string label = s.CountdownPhase switch
        {
            "freezetime" => Strings.Get("freezetime"),
            "live" => Strings.Get("live"),
            "over" => Strings.Get("over"),
            "warmup" => Strings.Get("warmup"),
            "paused" => Strings.Get("paused"),
            "bomb" => Strings.Get("bomb"),
            "defuse" => Strings.Get("defusePhase"),
            _ => Strings.Get("round"),
        };

        string value = "–";
        if (s.Connected && s.CountdownSeconds is double seconds)
        {
            double remaining = Math.Max(0, seconds - _gsi.MillisecondsSinceUpdate / 1000.0);
            value = remaining < 10 ? remaining.ToString("0.0")
                : remaining < 60 ? Math.Ceiling(remaining).ToString("0")
                : $"{(int)(remaining / 60)}:{(int)(remaining % 60):00}";
        }

        bool bomb = s.CountdownPhase == "bomb";
        DrawCard(g, rect, label, value, string.Empty,
            bomb ? Palette.Danger : accent,
            bomb ? Palette.Danger : Palette.Foreground);
    }

    private void DrawChips(Graphics g, Rectangle rect, OverlaySnapshot s, Color accent)
    {
        DrawPanel(g, rect, Palette.Edge);
        if (!s.Connected) return;

        int pad = (int)(_settings.Unit * 0.5);
        var parts = new List<(string Text, Color Colour)>
        {
            ($"${s.Money}", Palette.Foreground),
            ($"{s.Kills}/{s.Deaths}", Palette.Muted),
        };

        if (s.Grenades.Count > 0) parts.Add((string.Join(" ", s.Grenades), accent));
        if (s.DefuseKit) parts.Add((Strings.Get("kit"), Palette.Ok));
        if (s.HasBomb) parts.Add((Strings.Get("hasBomb"), Palette.Warn));

        float x = rect.X + pad;
        using var format = new StringFormat { LineAlignment = StringAlignment.Center };

        foreach (var (text, colour) in parts)
        {
            SizeF size = g.MeasureString(text, _noteFont);
            if (x + size.Width > rect.Right - pad) break;

            using var brush = new SolidBrush(colour);
            g.DrawString(text, _noteFont, brush,
                new RectangleF(x, rect.Y, size.Width, rect.Height), format);
            x += size.Width + pad;
        }
    }

    /// <summary>
    /// Left/right axis with a live needle and fading marks for recent transients.
    /// Deliberately one dimensional: the signal contains no front/back
    /// information, so drawing a compass would be inventing data.
    /// </summary>
    private void DrawSound(Graphics g, Rectangle rect, Color accent)
    {
        DrawPanel(g, rect, Palette.Edge);

        int pad = (int)(_settings.Unit * 0.6);
        var inner = new Rectangle(rect.X + pad, rect.Y + pad, rect.Width - pad * 2, rect.Height - pad * 2);
        int axisY = inner.Bottom - (int)(_settings.Unit * 0.9);

        var health = _sound.Health;
        if (health != SoundHealth.Ok && health != SoundHealth.Mono)
        {
            string message = health switch
            {
                SoundHealth.Starting => Strings.Get("soundStarting"),
                SoundHealth.Silent => Strings.Get("soundSilent"),
                SoundHealth.NoBlocks => Strings.Get("soundNoBlocks"),
                _ => Strings.Get("soundOff"),
            };

            using var brush = new SolidBrush(Palette.Muted);
            using var centre = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString(message, _noteFont, brush, inner, centre);
            return;
        }

        using var axisPen = new Pen(Palette.Edge, Math.Max(1, _settings.Unit / 14f));
        g.DrawLine(axisPen, inner.Left, axisY, inner.Right, axisY);
        g.DrawLine(axisPen, inner.Left + inner.Width / 2, axisY - _settings.Unit / 3,
            inner.Left + inner.Width / 2, axisY + _settings.Unit / 3);

        using var mutedBrush = new SolidBrush(Palette.Muted);
        using var left = new StringFormat { Alignment = StringAlignment.Near };
        using var right = new StringFormat { Alignment = StringAlignment.Far };
        var labelRect = new RectangleF(inner.Left, axisY + _settings.Unit * 0.1f,
            inner.Width, _settings.Unit * 0.9f);
        g.DrawString(Strings.Get("left"), _noteFont, mutedBrush, labelRect, left);
        g.DrawString(Strings.Get("right"), _noteFont, mutedBrush, labelRect, right);

        float XFor(double pan) =>
            inner.Left + (float)((Math.Clamp(pan, -1, 1) + 1) / 2) * inner.Width;

        // Needle height is loudness, so a quiet room does not look like a
        // constant signal.
        double loudness = Math.Clamp((_sound.LevelDb + 80) / 60.0, 0, 1);
        int usableHeight = axisY - inner.Top;
        using var needlePen = new Pen(accent, Math.Max(2, _settings.Unit / 7f));
        float needleX = XFor(_sound.Pan);
        g.DrawLine(needlePen, needleX, axisY, needleX, axisY - (float)(loudness * usableHeight));

        long now = Environment.TickCount64;
        foreach (var evt in _sound.RecentEvents())
        {
            double age = (now - evt.At) / 4000.0;
            if (age is < 0 or >= 1) continue;

            float radius = Math.Clamp(
                _settings.Unit * 0.18f + (float)Math.Log2(Math.Max(1, evt.Ratio)) * _settings.Unit * 0.1f,
                _settings.Unit * 0.14f, _settings.Unit * 0.5f);

            int alpha = (int)Math.Clamp(255 * (1 - age), 0, 255);
            Color colour = evt.Loud ? Palette.Danger : accent;
            using var brush = new SolidBrush(Color.FromArgb(alpha, colour));

            float cy = inner.Top + (float)(age * (usableHeight - radius));
            g.FillEllipse(brush, XFor(evt.Pan) - radius, cy - radius, radius * 2, radius * 2);
        }
    }

    // ------------------------------------------------------------ paint helpers

    private void DrawPanel(Graphics g, Rectangle rect, Color edge)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;

        int radius = Math.Max(2, (int)(_settings.Unit * 0.4));
        using var path = RoundedRect(rect, radius);
        using var fill = new SolidBrush(Palette.Background);
        using var pen = new Pen(edge, 1);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }

    private void DrawCard(Graphics g, Rectangle rect, string label, string value, string note,
        Color accent, Color valueColor)
    {
        DrawPanel(g, rect, Palette.Edge);

        // Accent stripe down the left edge: colour without tinting the text.
        int stripe = Math.Max(2, (int)(_settings.Unit * 0.16));
        using (var stripeBrush = new SolidBrush(accent))
        {
            g.FillRectangle(stripeBrush, rect.X + 1, rect.Y + 2, stripe, rect.Height - 4);
        }

        int pad = stripe + (int)(_settings.Unit * 0.4);
        var textRect = new Rectangle(rect.X + pad, rect.Y + (int)(_settings.Unit * 0.15),
            rect.Width - pad - 4, rect.Height - (int)(_settings.Unit * 0.3));

        using var labelBrush = new SolidBrush(Palette.Muted);
        using var valueBrush = new SolidBrush(valueColor);
        using var noteBrush = new SolidBrush(Palette.Muted);
        using var noWrap = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        g.DrawString(label, _labelFont, labelBrush,
            new RectangleF(textRect.X, textRect.Y, textRect.Width, _labelFont.Height), noWrap);

        g.DrawString(value, _valueFont, valueBrush,
            new RectangleF(textRect.X, textRect.Y + _labelFont.Height * 0.9f,
                textRect.Width, _valueFont.Height), noWrap);

        if (note.Length > 0)
        {
            g.DrawString(note, _noteFont, noteBrush,
                new RectangleF(textRect.X, textRect.Bottom - _noteFont.Height,
                    textRect.Width, _noteFont.Height), noWrap);
        }
    }

    private void DrawBar(Graphics g, Rectangle rect, string text, Color background, Color foreground)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;

        int radius = Math.Max(2, (int)(_settings.Unit * 0.35));
        using var path = RoundedRect(rect, radius);
        using var fill = new SolidBrush(background);
        g.FillPath(fill, path);

        using var brush = new SolidBrush(foreground);
        using var centre = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        g.DrawString(text, _alertFont, brush, rect, centre);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(1, radius * 2);

        if (d >= rect.Width || d >= rect.Height)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ------------------------------------------------------------------- tick

    private void OnTick(object? sender, EventArgs e)
    {
        _gsi.CheckStale();

        // A game going fullscreen pushes every other window down the z-order.
        Win32.PushToTop(Handle);

        if (_settings.OnlyOverGame)
        {
            string foreground = Win32.ForegroundProcessName();
            bool show = foreground is "cs2" || _settingsForm is { Visible: true };
            if (Visible != show) Visible = show;
        }
        else if (!Visible)
        {
            Visible = true;
        }

        // The size depends on how many alerts are up, so it can change between
        // frames rather than only when the settings do.
        var layout = ComputeLayout();
        if (layout.Height != Height || layout.Width != Width) ApplyGeometry();

        Invalidate();
    }

    // --------------------------------------------------------------- settings

    public void ToggleSettings()
    {
        if (_settingsForm is { IsDisposed: false, Visible: true })
        {
            _settingsForm.Hide();
            ApplyClickThrough(true);
            return;
        }

        if (_settingsForm is null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_settings, _sound, this);
        }

        // The menu needs the mouse, so click-through has to come off while it is
        // up - otherwise the clicks land in the game behind it.
        ApplyClickThrough(false);
        Visible = true;
        _settingsForm.ShowAtCentre();
    }

    /// <summary>Called by the settings window whenever the player changes something.</summary>
    public void SettingsChanged()
    {
        _settings.Save();
        ApplyGeometry();

        if (_settings.SoundEnabled && !_sound.Running) StartSound();
        else if (!_settings.SoundEnabled && _sound.Running) _sound.Stop();

        _sound.Sensitivity = _settings.SoundSensitivity;
    }

    private void StartSound()
    {
        _sound.Sensitivity = _settings.SoundSensitivity;
        if (_sound.Start()) return;

        TrayNotify($"{Strings.Get("audioError")}: {_sound.LastError}");
    }

    private void TrayNotify(string message) => TrayNotified?.Invoke(message);

    /// <summary>Set by Program so messages reach the tray icon.</summary>
    public Action<string>? TrayNotified { get; set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
            _hook.Dispose();
            _settingsForm?.Dispose();
            _labelFont?.Dispose();
            _valueFont?.Dispose();
            _noteFont?.Dispose();
            _alertFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}
