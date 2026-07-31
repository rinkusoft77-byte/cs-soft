using CounterStrikeSharp.API.Core;

namespace VisionAssist;

/// <summary>
/// Client-side convars that help a player with low vision. The server cannot
/// set these for someone, so they are either pushed with ExecuteClientCommand
/// (which the client may refuse) or printed for the player to paste into their
/// own autoexec.cfg.
/// </summary>
public static class ClientTips
{
    /// <summary>Bigger, more readable radar and HUD.</summary>
    public static readonly string[] RadarCommands =
    {
        "cl_radar_scale 0.4",             // zoom the radar in on the player
        "cl_radar_always_centered 0",     // use the whole radar square
        "cl_hud_radar_scale 1.3",         // draw the radar itself larger
        "cl_radar_icon_scale_min 0.6",    // larger player dots
        "cl_radar_rotate 1",
        "hud_scaling 0.95",               // largest HUD the game allows
    };

    /// <summary>A thick, bright crosshair - easier to keep track of.</summary>
    public static readonly string[] CrosshairCommands =
    {
        "cl_crosshairsize 4",
        "cl_crosshairthickness 1.6",
        "cl_crosshair_outlinethickness 1",
        "cl_crosshairgap -2",
        "cl_crosshaircolor 5",
        "cl_crosshaircolor_r 255",
        "cl_crosshaircolor_g 0",
        "cl_crosshaircolor_b 255",
        "cl_crosshairalpha 255",
        "cl_crosshairdot 1",
    };

    public static void ApplyRadar(CCSPlayerController player)
    {
        foreach (var command in RadarCommands) player.ExecuteClientCommand(command);
    }

    public static void PrintAll(CCSPlayerController player)
    {
        player.PrintToConsole("");
        player.PrintToConsole("// ---- VisionAssist: recommended client settings ----");
        player.PrintToConsole("// Paste into csgo/cfg/autoexec.cfg to keep them.");
        player.PrintToConsole("");
        player.PrintToConsole("// Radar and HUD");

        foreach (var command in RadarCommands) player.PrintToConsole(command);

        player.PrintToConsole("");
        player.PrintToConsole("// Crosshair");

        foreach (var command in CrosshairCommands) player.PrintToConsole(command);

        player.PrintToConsole("");
    }
}
