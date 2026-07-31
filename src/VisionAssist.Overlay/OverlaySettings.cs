using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisionAssist.Overlay;

internal enum OverlayCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>
/// Written to overlay.json next to the executable. Saved as soon as anything in
/// the menu changes, so a crash mid-match does not lose the settings.
/// </summary>
internal sealed class OverlaySettings
{
    [JsonPropertyName("Language")]
    public string Language { get; set; } = "uz";

    [JsonPropertyName("Theme")]
    public string Theme { get; set; } = "highcontrast";

    /// <summary>s, m, l or xl.</summary>
    [JsonPropertyName("Size")]
    public string Size { get; set; } = "l";

    [JsonPropertyName("Corner")]
    public OverlayCorner Corner { get; set; } = OverlayCorner.TopLeft;

    /// <summary>0.35 to 1.0. Lower means less of the game is covered up.</summary>
    [JsonPropertyName("Opacity")]
    public double Opacity { get; set; } = 0.88;

    [JsonPropertyName("SoundEnabled")]
    public bool SoundEnabled { get; set; } = true;

    [JsonPropertyName("SoundSensitivity")]
    public double SoundSensitivity { get; set; } = 0.5;

    /// <summary>Health at or below which the number turns red.</summary>
    [JsonPropertyName("LowHealthThreshold")]
    public int LowHealthThreshold { get; set; } = 35;

    /// <summary>
    /// Hide the HUD whenever CS2 is not the window in front. Off means it stays
    /// on top of everything, which is what you want while setting it up.
    /// </summary>
    [JsonPropertyName("OnlyOverGame")]
    public bool OnlyOverGame { get; set; } = true;

    [JsonPropertyName("Port")]
    public int Port { get; set; } = 47474;

    [JsonPropertyName("Token")]
    public string Token { get; set; } = "visionassist-local";

    // ------------------------------------------------------------------ sizes

    /// <summary>Base unit in pixels for the current size setting.</summary>
    [JsonIgnore]
    public int Unit => Size?.ToLowerInvariant() switch
    {
        "s" => 13,
        "m" => 17,
        "xl" => 28,
        _ => 21,
    };

    public static readonly string[] Sizes = { "s", "m", "l", "xl" };

    // ------------------------------------------------------------- load, save

    private static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, "overlay.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static OverlaySettings Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var loaded = JsonSerializer.Deserialize<OverlaySettings>(File.ReadAllText(Path), Options);
                if (loaded is not null) return loaded.Clamped();
            }
        }
        catch (Exception)
        {
            // Corrupt or unreadable: the defaults are better than a crash on start.
        }

        return new OverlaySettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(this, Options));
        }
        catch (Exception)
        {
            // Read-only folder. The setting still applies for this run.
        }
    }

    private OverlaySettings Clamped()
    {
        Opacity = Math.Clamp(Opacity, 0.35, 1.0);
        SoundSensitivity = Math.Clamp(SoundSensitivity, 0, 1);
        LowHealthThreshold = Math.Clamp(LowHealthThreshold, 1, 99);
        if (Port is < 1 or > 65535) Port = 47474;
        if (!Strings.Languages.Contains(Language)) Language = "uz";

        Size ??= "l";
        if (!Sizes.Contains(Size.ToLowerInvariant())) Size = "l";
        return this;
    }
}
