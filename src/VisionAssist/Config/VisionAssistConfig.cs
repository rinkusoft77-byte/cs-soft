using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace VisionAssist;

/// <summary>
/// Written to addons/counterstrikesharp/configs/plugins/VisionAssist/VisionAssist.json
/// on first load. Colours accept "#RRGGBB", "r,g,b" or a name from
/// <see cref="ColorParser.NamedColors"/>.
/// </summary>
public class VisionAssistConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 2;

    // ---------------------------------------------------------- model colour

    /// <summary>Recolours the player model itself. Only visible where the model is.</summary>
    [JsonPropertyName("TintEnabled")]
    public bool TintEnabled { get; set; } = true;

    [JsonPropertyName("TintColorT")]
    public string TintColorT { get; set; } = "#FFEB3C";

    [JsonPropertyName("TintColorCT")]
    public string TintColorCT { get; set; } = "#00D9FF";

    /// <summary>1 = almost invisible, 255 = fully solid.</summary>
    [JsonPropertyName("TintAlpha")]
    public int TintAlpha { get; set; } = 255;

    /// <summary>
    /// Blends the model colour towards <see cref="TintHurtColor"/> as the player
    /// loses health, so a wounded player is readable at a glance.
    /// </summary>
    [JsonPropertyName("TintFollowsHealth")]
    public bool TintFollowsHealth { get; set; } = false;

    [JsonPropertyName("TintHurtColor")]
    public string TintHurtColor { get; set; } = "#FF2020";

    // -------------------------------------------------------------- outline

    /// <summary>
    /// Draws an outline around the player model. This deliberately has no
    /// see-through-walls mode: the outline is an edge on a model you can
    /// already see, not an X-ray.
    /// </summary>
    [JsonPropertyName("OutlineEnabled")]
    public bool OutlineEnabled { get; set; } = true;

    [JsonPropertyName("OutlineColorT")]
    public string OutlineColorT { get; set; } = "#FFEB3C";

    [JsonPropertyName("OutlineColorCT")]
    public string OutlineColorCT { get; set; } = "#00D9FF";

    /// <summary>Distance in units at which the outline stops drawing.</summary>
    [JsonPropertyName("OutlineRange")]
    public int OutlineRange { get; set; } = 4000;

    /// <summary>
    /// CGlowProperty::m_iGlowType. 2 keeps the outline on visible models only,
    /// which is what this plugin is for. 3 is the through-walls mode and is
    /// intentionally not offered as a toggle; the field is here only so the
    /// value can be corrected if a game update renumbers the modes.
    /// </summary>
    [JsonPropertyName("OutlineGlowType")]
    public int OutlineGlowType { get; set; } = 2;

    // ------------------------------------------------------------ highlights

    /// <summary>Recolours objectives so they stand out from the map.</summary>
    [JsonPropertyName("HighlightEnabled")]
    public bool HighlightEnabled { get; set; } = true;

    [JsonPropertyName("HighlightBombColor")]
    public string HighlightBombColor { get; set; } = "#FF3B30";

    [JsonPropertyName("HighlightDefuserColor")]
    public string HighlightDefuserColor { get; set; } = "#00E5FF";

    [JsonPropertyName("HighlightHostageColor")]
    public string HighlightHostageColor { get; set; } = "#FFD400";

    [JsonPropertyName("HighlightInterval")]
    public float HighlightInterval { get; set; } = 1.0f;

    // ---------------------------------------------------------------- radar

    [JsonPropertyName("RadarEnabled")]
    public bool RadarEnabled { get; set; } = true;

    /// <summary>The engine recomputes spotted state constantly, so this repeats.</summary>
    [JsonPropertyName("RadarUpdateInterval")]
    public float RadarUpdateInterval { get; set; } = 0.35f;

    [JsonPropertyName("RadarIncludeDead")]
    public bool RadarIncludeDead { get; set; } = false;

    // ------------------------------------------------------------------ HUD

    /// <summary>Large centre-screen readout. Each player can turn it off for themselves.</summary>
    [JsonPropertyName("HudEnabled")]
    public bool HudEnabled { get; set; } = true;

    [JsonPropertyName("HudUpdateInterval")]
    public float HudUpdateInterval { get; set; } = 0.2f;

    /// <summary>Default size for players who have not picked one: s, m or l.</summary>
    [JsonPropertyName("HudDefaultSize")]
    public string HudDefaultSize { get; set; } = "l";

    [JsonPropertyName("HudColor")]
    public string HudColor { get; set; } = "#FFFFFF";

    [JsonPropertyName("HudLowHealthColor")]
    public string HudLowHealthColor { get; set; } = "#FF3B30";

    [JsonPropertyName("HudLowHealthThreshold")]
    public int HudLowHealthThreshold { get; set; } = 35;

    [JsonPropertyName("HudShowHealth")]
    public bool HudShowHealth { get; set; } = true;

    [JsonPropertyName("HudShowArmor")]
    public bool HudShowArmor { get; set; } = true;

    [JsonPropertyName("HudShowRoundTimer")]
    public bool HudShowRoundTimer { get; set; } = true;

    [JsonPropertyName("HudShowBombTimer")]
    public bool HudShowBombTimer { get; set; } = true;

    [JsonPropertyName("HudShowAliveCount")]
    public bool HudShowAliveCount { get; set; } = true;

    // -------------------------------------------------------------- general

    /// <summary>Applied on first load only; after that the colours above win.</summary>
    [JsonPropertyName("DefaultTheme")]
    public string DefaultTheme { get; set; } = "highcontrast";

    /// <summary>uz, ru or en. Players can override this for themselves.</summary>
    [JsonPropertyName("DefaultLanguage")]
    public string DefaultLanguage { get; set; } = "uz";

    [JsonPropertyName("AnnounceOnJoin")]
    public bool AnnounceOnJoin { get; set; } = true;

    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = " {purple}[Vision]{default}";

    /// <summary>Permission flag required for the settings commands.</summary>
    [JsonPropertyName("AdminFlag")]
    public string AdminFlag { get; set; } = "@css/generic";
}
