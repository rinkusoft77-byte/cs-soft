using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisionAssist.Companion;

/// <summary>
/// Written to companion.json next to the executable on first run. The overlay
/// page reads the display-related keys back over HTTP, so changing a colour or
/// a font size only needs a page refresh, not a rebuild.
/// </summary>
public sealed class CompanionConfig
{
    /// <summary>Loopback port for both the GSI endpoint and the overlay page.</summary>
    [JsonPropertyName("Port")]
    public int Port { get; set; } = 47474;

    /// <summary>
    /// Shared secret. Must match the token in
    /// gamestate_integration_visionassist.cfg; the game sends it with every
    /// payload so nothing else on the machine can feed the overlay.
    /// </summary>
    [JsonPropertyName("Token")]
    public string Token { get; set; } = "visionassist-local";

    /// <summary>Set to false only when debugging a hand-made POST.</summary>
    [JsonPropertyName("RequireToken")]
    public bool RequireToken { get; set; } = true;

    /// <summary>Colour preset key. Same seven presets as the server plugin.</summary>
    [JsonPropertyName("DefaultTheme")]
    public string DefaultTheme { get; set; } = "highcontrast";

    /// <summary>Overlay text size: <c>s</c>, <c>m</c>, <c>l</c> or <c>xl</c>.</summary>
    [JsonPropertyName("DefaultSize")]
    public string DefaultSize { get; set; } = "l";

    /// <summary>Health at or below which the number turns into a warning colour.</summary>
    [JsonPropertyName("LowHealthThreshold")]
    public int LowHealthThreshold { get; set; } = 35;

    /// <summary>Draw the sound direction panel at all.</summary>
    [JsonPropertyName("SoundIndicatorEnabled")]
    public bool SoundIndicatorEnabled { get; set; } = true;

    /// <summary>
    /// 0.1 = only obvious sounds register, 1.0 = very twitchy. Adjustable from
    /// the page as well; this is just the starting value.
    /// </summary>
    [JsonPropertyName("SoundSensitivity")]
    public double SoundSensitivity { get; set; } = 0.5;

    /// <summary>
    /// Transparent page background, for stacking the window on top of a
    /// borderless-windowed game. Off by default because a solid background is
    /// what you want on a second monitor.
    /// </summary>
    [JsonPropertyName("TransparentBackground")]
    public bool TransparentBackground { get; set; } = false;

    /// <summary>Launch the default browser on the overlay URL at startup.</summary>
    [JsonPropertyName("OpenBrowserOnStart")]
    public bool OpenBrowserOnStart { get; set; } = true;

    // ------------------------------------------------------------------ load

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "companion.json");

    private static readonly JsonSerializerOptions FileOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Reads companion.json, creating it with the defaults when missing. A file
    /// that cannot be parsed is reported and the defaults are used, so a stray
    /// comma never stops the overlay from starting.
    /// </summary>
    public static CompanionConfig Load(string path, out string? notice)
    {
        notice = null;

        if (!File.Exists(path))
        {
            var fresh = new CompanionConfig();
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(fresh, FileOptions));
                notice = $"Created {path}";
            }
            catch (Exception ex)
            {
                notice = $"Could not write {path}: {ex.Message} (using defaults)";
            }
            return fresh;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<CompanionConfig>(File.ReadAllText(path), FileOptions);
            if (loaded is not null) return loaded;
            notice = $"{path} is empty - using defaults";
        }
        catch (Exception ex)
        {
            notice = $"Could not read {path}: {ex.Message} (using defaults)";
        }

        return new CompanionConfig();
    }

    /// <summary>The subset the overlay page needs. Never includes the token.</summary>
    public object ToClientView() => new
    {
        theme = DefaultTheme,
        size = DefaultSize,
        lowHealthThreshold = LowHealthThreshold,
        soundIndicatorEnabled = SoundIndicatorEnabled,
        soundSensitivity = SoundSensitivity,
        transparentBackground = TransparentBackground,
    };
}
