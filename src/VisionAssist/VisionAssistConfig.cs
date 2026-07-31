using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace VisionAssist;

/// <summary>
/// Written to addons/counterstrikesharp/configs/plugins/VisionAssist/VisionAssist.json
/// the first time the plugin loads. Colors accept "#RRGGBB", "r,g,b" or a name
/// from <see cref="ColorParser.NamedColors"/>.
/// </summary>
public class VisionAssistConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;

    // ---- Glow (outline around the player model) ----

    [JsonPropertyName("GlowEnabled")]
    public bool GlowEnabled { get; set; } = true;

    /// <summary>
    /// true  -> outline stays visible when the player is behind geometry (glow type 3).
    /// false -> outline only draws on players you can already see (glow type 2).
    /// </summary>
    [JsonPropertyName("GlowThroughWalls")]
    public bool GlowThroughWalls { get; set; } = true;

    [JsonPropertyName("GlowColorT")]
    public string GlowColorT { get; set; } = "#FF2ED1";

    [JsonPropertyName("GlowColorCT")]
    public string GlowColorCT { get; set; } = "#00D9FF";

    /// <summary>Distance in units at which the glow stops drawing.</summary>
    [JsonPropertyName("GlowRange")]
    public int GlowRange { get; set; } = 5000;

    /// <summary>Distance below which the glow is suppressed. 0 = always draw.</summary>
    [JsonPropertyName("GlowRangeMin")]
    public int GlowRangeMin { get; set; } = 0;

    // ---- Model tint (recolours the player model itself) ----

    [JsonPropertyName("TintEnabled")]
    public bool TintEnabled { get; set; } = true;

    [JsonPropertyName("TintColorT")]
    public string TintColorT { get; set; } = "#FF2ED1";

    [JsonPropertyName("TintColorCT")]
    public string TintColorCT { get; set; } = "#00D9FF";

    /// <summary>0 = invisible, 255 = fully solid.</summary>
    [JsonPropertyName("TintAlpha")]
    public int TintAlpha { get; set; } = 255;

    // ---- Radar ----

    [JsonPropertyName("RadarEnabled")]
    public bool RadarEnabled { get; set; } = true;

    /// <summary>
    /// How often (seconds) the spotted flag is refreshed. The engine clears it
    /// continuously, so anything above ~1.0 makes radar blips flicker.
    /// </summary>
    [JsonPropertyName("RadarUpdateInterval")]
    public float RadarUpdateInterval { get; set; } = 0.35f;

    /// <summary>Keep dead players' blips on the radar as well.</summary>
    [JsonPropertyName("RadarIncludeDead")]
    public bool RadarIncludeDead { get; set; } = false;

    // ---- Misc ----

    /// <summary>
    /// Tells every connecting player that enhanced visibility is active on this
    /// server. Leave this on — the effects are shared by everyone, and players
    /// should know what they are joining.
    /// </summary>
    [JsonPropertyName("AnnounceOnJoin")]
    public bool AnnounceOnJoin { get; set; } = true;

    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = " {purple}[Vision]{default}";

    /// <summary>CounterStrikeSharp permission flag required for the admin commands.</summary>
    [JsonPropertyName("AdminFlag")]
    public string AdminFlag { get; set; } = "@css/generic";
}
