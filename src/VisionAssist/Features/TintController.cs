using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VisionAssist;

/// <summary>
/// Recolours the player model through CBaseModelEntity::m_clrRender. This is
/// the core feature for low vision: it repaints the model that is already on
/// screen, so a player never becomes visible anywhere they were not already.
/// </summary>
public sealed class TintController
{
    private readonly VisionAssistPlugin _plugin;

    public TintController(VisionAssistPlugin plugin) => _plugin = plugin;

    private VisionAssistConfig Config => _plugin.Config;

    public void ApplyToAll()
    {
        foreach (var player in Utilities.GetPlayers()) Apply(player);
    }

    public void Apply(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid || player.IsHLTV) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return;

        if (!Config.TintEnabled)
        {
            Reset(pawn);
            return;
        }

        var color = ColorFor(player.TeamNum, pawn.Health, pawn.MaxHealth);
        var alpha = Math.Clamp(Config.TintAlpha, 1, 255);

        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        pawn.Render = Color.FromArgb(alpha, color.R, color.G, color.B);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    /// <summary>Only useful while TintFollowsHealth is on; otherwise nothing changes.</summary>
    public void RefreshHealthShading()
    {
        if (!Config.TintEnabled || !Config.TintFollowsHealth) return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && player.PawnIsAlive) Apply(player);
        }
    }

    public Color BaseColorFor(int teamNum) => teamNum == (int)CsTeam.CounterTerrorist
        ? ColorParser.ParseOrDefault(Config.TintColorCT, Color.Cyan)
        : ColorParser.ParseOrDefault(Config.TintColorT, Color.Yellow);

    private Color ColorFor(int teamNum, int health, int maxHealth)
    {
        var baseColor = BaseColorFor(teamNum);
        if (!Config.TintFollowsHealth) return baseColor;

        var max = maxHealth <= 0 ? 100 : maxHealth;
        var missing = 1f - Math.Clamp((float)health / max, 0f, 1f);

        // Only shade the wounded half of the range, so a healthy player keeps
        // the team colour exactly as configured.
        var hurt = ColorParser.ParseOrDefault(Config.TintHurtColor, Color.Red);
        return ColorParser.Blend(baseColor, hurt, missing * 0.85f);
    }

    public void ResetAll()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.IsValid ? player.PlayerPawn.Value : null;
            if (pawn is not null && pawn.IsValid) Reset(pawn);
        }
    }

    private static void Reset(CCSPlayerPawn pawn)
    {
        pawn.RenderMode = RenderMode_t.kRenderNormal;
        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}
