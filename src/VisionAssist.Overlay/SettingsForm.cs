using System.Drawing;
using System.Windows.Forms;

namespace VisionAssist.Overlay;

/// <summary>
/// The menu that opens on an Alt tap.
///
/// Ordinary WinForms controls on an ordinary window, deliberately: a
/// custom-painted menu inside the click-through overlay would mean writing hit
/// testing and keyboard handling by hand, and combo boxes that already work are
/// worth more here than a matching visual style.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly OverlaySettings _settings;
    private readonly SoundMeter _sound;
    private readonly OverlayForm _overlay;

    // These live for the lifetime of the window. Their values survive a language
    // change, which rebuilds the labels around them.
    private readonly ComboBox _language = new();
    private readonly ComboBox _theme = new();
    private readonly ComboBox _size = new();
    private readonly ComboBox _corner = new();
    private readonly TrackBar _opacity = new();
    private readonly CheckBox _soundEnabled = new();
    private readonly TrackBar _sensitivity = new();
    private readonly CheckBox _onlyOverGame = new();
    private readonly CheckBox _edgeFlash = new();
    private readonly CheckBox _speech = new();
    private readonly TrackBar _speechRate = new();

    /// <summary>
    /// Labels and buttons created by <see cref="BuildControls"/>. Tracked so a
    /// rebuild can dispose them instead of leaking a GDI handle per language
    /// change.
    /// </summary>
    private readonly List<Control> _owned = new();

    /// <summary>Suppresses change handlers while the controls are being filled in.</summary>
    private bool _loading = true;

    public SettingsForm(OverlaySettings settings, SoundMeter sound, OverlayForm overlay)
    {
        _settings = settings;
        _sound = sound;
        _overlay = overlay;

        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.Manual;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(0x0D, 0x11, 0x16);
        ForeColor = Palette.Foreground;
        Font = new Font("Segoe UI", 10f);

        StyleInputs();

        // Wired once, here rather than in BuildControls: the controls are
        // re-parented on a language change, and handlers attached during a
        // rebuild would accumulate.
        _language.SelectedIndexChanged += (_, _) => OnChanged();
        _theme.SelectedIndexChanged += (_, _) => OnChanged();
        _size.SelectedIndexChanged += (_, _) => OnChanged();
        _corner.SelectedIndexChanged += (_, _) => OnChanged();
        _opacity.Scroll += (_, _) => OnChanged();
        _sensitivity.Scroll += (_, _) => OnChanged();
        _speechRate.Scroll += (_, _) => OnChanged();
        _soundEnabled.CheckedChanged += (_, _) => OnChanged();
        _onlyOverGame.CheckedChanged += (_, _) => OnChanged();
        _edgeFlash.CheckedChanged += (_, _) => OnChanged();
        _speech.CheckedChanged += (_, _) => OnChanged();

        BuildControls();
        LoadValues();
        _loading = false;

        // Closing with the X hides instead of disposing: the same instance is
        // reused every time Alt is tapped.
        FormClosing += (_, e) =>
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                Hide();
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Hide();
        };
    }

    public void ShowAtCentre()
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(
            screen.Left + (screen.Width - Width) / 2,
            screen.Top + (screen.Height - Height) / 2);

        Show();
        BringToFront();
        Activate();
    }

    // ------------------------------------------------------------------ build

    private void StyleInputs()
    {
        foreach (var combo in new[] { _language, _theme, _size, _corner })
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Color.FromArgb(0x12, 0x16, 0x1C);
            combo.ForeColor = Palette.Foreground;
            combo.Size = new Size(328, 24);
        }

        foreach (var slider in new[] { _opacity, _sensitivity, _speechRate })
        {
            slider.TickStyle = TickStyle.None;
            slider.Size = new Size(332, 28);
            slider.BackColor = BackColor;
        }

        _opacity.Minimum = 35;
        _opacity.Maximum = 100;
        _sensitivity.Minimum = 0;
        _sensitivity.Maximum = 100;
        _speechRate.Minimum = 0;
        _speechRate.Maximum = 100;

        foreach (var check in new[] { _soundEnabled, _onlyOverGame, _edgeFlash, _speech })
        {
            check.ForeColor = Palette.Foreground;
            check.AutoSize = true;
        }
    }

    private void BuildControls()
    {
        Text = "VisionAssist — " + Strings.Get("settings");

        int y = 12;

        y = AddOwnedLabel(Strings.Get("settings"), y, Palette.Foreground,
            new Font("Segoe UI", 13f, FontStyle.Bold)) + 8;

        y = AddOwnedLabel(Strings.Get("settingsHint"), y, Palette.Muted, null) + 8;

        y = AddField(_language, Strings.Get("language"), y,
            Strings.Languages.Select(LanguageName).ToArray());

        y = AddField(_theme, Strings.Get("theme"), y,
            Palette.All.Select(p => $"{p.Key} — {p.Description}").ToArray());

        y = AddField(_size, Strings.Get("size"), y,
            OverlaySettings.Sizes.Select(s => s.ToUpperInvariant()).ToArray());

        y = AddField(_corner, Strings.Get("corner"), y, new[]
        {
            Strings.Get("topLeft"),
            Strings.Get("topRight"),
            Strings.Get("bottomLeft"),
            Strings.Get("bottomRight"),
        });

        y = AddField(_opacity, Strings.Get("opacity"), y, null);

        _soundEnabled.Text = Strings.Get("sound");
        _soundEnabled.Location = new Point(16, y);
        Controls.Add(_soundEnabled);
        y += 28;

        y = AddField(_sensitivity, Strings.Get("sensitivity"), y, null);

        _onlyOverGame.Text = OnlyOverGameLabel();
        _onlyOverGame.Location = new Point(16, y);
        Controls.Add(_onlyOverGame);
        y += 34;

        // Two accessibility sections, labelled by who they are for rather than by
        // what they technically do - that is how someone picking settings for a
        // specific difficulty will look for them.
        y = AddSectionHeader(Strings.Get("forDeaf"), y);

        _edgeFlash.Text = Strings.Get("edgeFlash");
        _edgeFlash.Location = new Point(16, y);
        Controls.Add(_edgeFlash);
        y += 34;

        y = AddSectionHeader(Strings.Get("forBlind"), y);

        _speech.Text = Strings.Get("speech");
        _speech.Location = new Point(16, y);
        Controls.Add(_speech);
        y += 30;

        y = AddField(_speechRate, Strings.Get("speechRate"), y, null);

        y = AddOwnedLabel(Strings.Get("onlyOwnData"), y, Palette.Muted, null, height: 36) + 8;

        var close = new Button
        {
            Text = Strings.Get("close"),
            Location = new Point(16, y),
            Size = new Size(150, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x1B, 0x21, 0x29),
            ForeColor = Palette.Foreground,
        };
        close.Click += (_, _) => Hide();
        Controls.Add(close);
        _owned.Add(close);
        CancelButton = close;

        var quit = new Button
        {
            Text = Strings.Get("quit"),
            Location = new Point(180, y),
            Size = new Size(164, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x2A, 0x14, 0x16),
            ForeColor = Palette.Danger,
        };
        quit.Click += (_, _) => Application.Exit();
        Controls.Add(quit);
        _owned.Add(quit);

        ClientSize = new Size(360, y + 44);
    }

    /// <summary>Adds a caption plus its input, returning the next free y.</summary>
    private int AddField(Control input, string caption, int y, string[]? items)
    {
        AddOwnedLabel(caption, y, Palette.Muted, null);

        if (items is not null && input is ComboBox combo)
        {
            // Cleared first: on a rebuild the same instance is refilled, and
            // adding again would duplicate every entry.
            combo.Items.Clear();
            combo.Items.AddRange(items);
        }

        input.Location = new Point(input is TrackBar ? 14 : 16, y + 20);
        Controls.Add(input);

        return y + 52;
    }

    /// <summary>A divider plus a caption, to break the list into sections.</summary>
    private int AddSectionHeader(string text, int y)
    {
        var rule = new Label
        {
            AutoSize = false,
            Size = new Size(328, 1),
            Location = new Point(16, y),
            BackColor = Palette.Edge,
        };
        Controls.Add(rule);
        _owned.Add(rule);

        return AddOwnedLabel(text, y + 9, Palette.Muted,
            new Font("Segoe UI", 9f, FontStyle.Bold)) + 4;
    }

    private int AddOwnedLabel(string text, int y, Color colour, Font? font, int height = 20)
    {
        var label = new Label
        {
            Text = text,
            ForeColor = colour,
            AutoSize = false,
            Size = new Size(330, height),
            Location = new Point(16, y),
        };
        if (font is not null) label.Font = font;

        Controls.Add(label);
        _owned.Add(label);

        return y + height;
    }

    private static string OnlyOverGameLabel() => Strings.Language switch
    {
        "ru" => "Показывать только когда CS2 впереди",
        "en" => "Only show while CS2 is in front",
        _ => "Faqat CS2 oldinda bo'lganda ko'rinsin",
    };

    private static string LanguageName(string code) => code switch
    {
        "ru" => "Русский",
        "en" => "English",
        _ => "O'zbekcha",
    };

    // ------------------------------------------------------------------ values

    private void LoadValues()
    {
        _language.SelectedIndex = Math.Max(0, Array.IndexOf(Strings.Languages, _settings.Language));

        int themeIndex = 0;
        for (int i = 0; i < Palette.All.Count; i++)
        {
            if (string.Equals(Palette.All[i].Key, _settings.Theme, StringComparison.OrdinalIgnoreCase))
            {
                themeIndex = i;
                break;
            }
        }
        _theme.SelectedIndex = themeIndex;

        _size.SelectedIndex = Math.Max(0,
            Array.IndexOf(OverlaySettings.Sizes, _settings.Size.ToLowerInvariant()));
        _corner.SelectedIndex = (int)_settings.Corner;

        _opacity.Value = Math.Clamp((int)Math.Round(_settings.Opacity * 100),
            _opacity.Minimum, _opacity.Maximum);
        _soundEnabled.Checked = _settings.SoundEnabled;
        _sensitivity.Value = Math.Clamp((int)Math.Round(_settings.SoundSensitivity * 100), 0, 100);
        _onlyOverGame.Checked = _settings.OnlyOverGame;
        _edgeFlash.Checked = _settings.EdgeFlashEnabled;
        _speech.Checked = _settings.SpeechEnabled;
        _speechRate.Value = Math.Clamp((int)Math.Round(_settings.SpeechRate * 100), 0, 100);
    }

    private void OnChanged()
    {
        if (_loading) return;

        string previousLanguage = _settings.Language;

        if (_language.SelectedIndex >= 0) _settings.Language = Strings.Languages[_language.SelectedIndex];
        if (_theme.SelectedIndex >= 0) _settings.Theme = Palette.All[_theme.SelectedIndex].Key;
        if (_size.SelectedIndex >= 0) _settings.Size = OverlaySettings.Sizes[_size.SelectedIndex];
        if (_corner.SelectedIndex >= 0) _settings.Corner = (OverlayCorner)_corner.SelectedIndex;

        _settings.Opacity = _opacity.Value / 100.0;
        _settings.SoundEnabled = _soundEnabled.Checked;
        _settings.SoundSensitivity = _sensitivity.Value / 100.0;
        _settings.OnlyOverGame = _onlyOverGame.Checked;
        _settings.EdgeFlashEnabled = _edgeFlash.Checked;
        _settings.SpeechEnabled = _speech.Checked;
        _settings.SpeechRate = _speechRate.Value / 100.0;

        Strings.Language = _settings.Language;
        _sound.Sensitivity = _settings.SoundSensitivity;

        _overlay.SettingsChanged();

        if (_settings.Language != previousLanguage) RebuildForLanguage();
    }

    /// <summary>
    /// Every caption here is translated, so a language change rebuilds them
    /// rather than patching each one. The inputs themselves are kept.
    /// </summary>
    private void RebuildForLanguage()
    {
        _loading = true;
        SuspendLayout();

        foreach (var control in _owned)
        {
            Controls.Remove(control);
            control.Dispose();
        }
        _owned.Clear();

        BuildControls();
        LoadValues();

        ResumeLayout(true);
        _loading = false;
    }
}
