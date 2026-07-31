using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VisionAssist;

/// <summary>
/// Gives every player a coloured outline and (optionally) recolours the player
/// model itself.
///
/// CS2 has no server-side "draw an outline on this player" input, so the usual
/// trick is used: two prop_dynamic copies of the player's own model are spawned
/// and parented to the player.
///   * relay prop - render mode "none", follows the player pawn. It exists only
///     to give the glow prop something to follow, because a prop cannot follow a
///     player pawn directly without inheriting its animation oddities.
///   * glow prop  - follows the relay and carries the CGlowProperty fields.
/// Both are networked to everyone, so the outline looks identical for every
/// player on the server. There is deliberately no per-viewer filtering here.
/// </summary>
public sealed class GlowController
{
    private readonly VisionAssistPlugin _plugin;

    // playerSlot -> the two helper entities spawned for that player.
    private readonly Dictionary<int, GlowEntities> _active = new();

    public GlowController(VisionAssistPlugin plugin) => _plugin = plugin;

    private VisionAssistConfig Config => _plugin.Config;

    private readonly record struct GlowEntities(uint RelayIndex, uint GlowIndex);

    /// <summary>Re-applies glow and tint for every alive player.</summary>
    public void ApplyToAll()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            Apply(player);
        }
    }

    public void Apply(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid || player.IsHLTV) return;
        if (!player.PawnIsAlive) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return;

        // A respawn reuses the slot, so always drop the previous props first.
        Clear(player.Slot);

        ApplyTint(pawn, TintColorFor(player.TeamNum));

        if (Config.GlowEnabled)
        {
            ApplyGlow(player, pawn, GlowColorFor(player.TeamNum));
        }
    }

    public Color GlowColorFor(int teamNum) => teamNum == (int)CsTeam.CounterTerrorist
        ? ColorParser.ParseOrDefault(Config.GlowColorCT, Color.Cyan)
        : ColorParser.ParseOrDefault(Config.GlowColorT, Color.Magenta);

    public Color TintColorFor(int teamNum) => teamNum == (int)CsTeam.CounterTerrorist
        ? ColorParser.ParseOrDefault(Config.TintColorCT, Color.Cyan)
        : ColorParser.ParseOrDefault(Config.TintColorT, Color.Magenta);

    private void ApplyTint(CCSPlayerPawn pawn, Color color)
    {
        if (!Config.TintEnabled)
        {
            ResetTint(pawn);
            return;
        }

        var alpha = Math.Clamp(Config.TintAlpha, 1, 255);

        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        pawn.Render = Color.FromArgb(alpha, color.R, color.G, color.B);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private static void ResetTint(CCSPlayerPawn pawn)
    {
        pawn.RenderMode = RenderMode_t.kRenderNormal;
        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private void ApplyGlow(CCSPlayerController player, CCSPlayerPawn pawn, Color color)
    {
        var modelName = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;
        if (string.IsNullOrEmpty(modelName)) return;

        var relay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (relay is null || !relay.IsValid || glow is null || !glow.IsValid)
        {
            relay?.AcceptInput("Kill");
            glow?.AcceptInput("Kill");
            return;
        }

        // 256 = SF_DYNAMICPROP_NO_VPHYSICS: no collision, so the copies never
        // block movement or shots.
        relay.Spawnflags = 256u;
        relay.RenderMode = RenderMode_t.kRenderNone;
        relay.SetModel(modelName);
        relay.DispatchSpawn();

        glow.Spawnflags = 256u;
        glow.SetModel(modelName);
        glow.DispatchSpawn();

        glow.Glow.GlowColorOverride = color;
        glow.Glow.GlowTeam = -1;                                  // -1 = visible to both teams
        glow.Glow.GlowType = Config.GlowThroughWalls ? 3 : 2;     // 3 = draws through geometry
        glow.Glow.GlowRange = Math.Max(0, Config.GlowRange);
        glow.Glow.GlowRangeMin = Math.Max(0, Config.GlowRangeMin);
        glow.Glow.Glowing = true;
        Utilities.SetStateChanged(glow, "CBaseModelEntity", "m_Glow");

        // FollowEntity with "!activator" makes the caller follow the activator.
        relay.AcceptInput("FollowEntity", pawn, relay, "!activator");
        glow.AcceptInput("FollowEntity", relay, glow, "!activator");

        _active[player.Slot] = new GlowEntities(relay.Index, glow.Index);
    }

    /// <summary>Pushes new colours onto the props that already exist, no respawn needed.</summary>
    public void RefreshColors()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn is null || !pawn.IsValid) continue;

            ApplyTint(pawn, TintColorFor(player.TeamNum));

            if (!Config.GlowEnabled)
            {
                Clear(player.Slot);
                continue;
            }

            if (!_active.TryGetValue(player.Slot, out var entities))
            {
                Apply(player);
                continue;
            }

            var glow = Utilities.GetEntityFromIndex<CDynamicProp>((int)entities.GlowIndex);
            if (glow is null || !glow.IsValid)
            {
                Apply(player);
                continue;
            }

            glow.Glow.GlowColorOverride = GlowColorFor(player.TeamNum);
            glow.Glow.GlowType = Config.GlowThroughWalls ? 3 : 2;
            glow.Glow.GlowRange = Math.Max(0, Config.GlowRange);
            glow.Glow.GlowRangeMin = Math.Max(0, Config.GlowRangeMin);
            glow.Glow.Glowing = true;
            Utilities.SetStateChanged(glow, "CBaseModelEntity", "m_Glow");
        }
    }

    public void Clear(int playerSlot)
    {
        if (!_active.Remove(playerSlot, out var entities)) return;

        KillEntity(entities.GlowIndex);
        KillEntity(entities.RelayIndex);
    }

    public void ClearAll()
    {
        foreach (var slot in _active.Keys.ToList())
        {
            Clear(slot);
        }

        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.IsValid ? player.PlayerPawn.Value : null;
            if (pawn is not null && pawn.IsValid) ResetTint(pawn);
        }
    }

    /// <summary>
    /// Drops the bookkeeping without touching the entities. Used on round restart,
    /// where the engine already wiped every non-preserved entity and the indices we
    /// stored may since have been handed to something else.
    /// </summary>
    public void Forget(int playerSlot) => _active.Remove(playerSlot);

    public void ForgetAll() => _active.Clear();

    private static void KillEntity(uint index)
    {
        var entity = Utilities.GetEntityFromIndex<CDynamicProp>((int)index);

        // Indices get recycled, so confirm this is still one of our props.
        if (entity is null || !entity.IsValid) return;
        if (entity.DesignerName != "prop_dynamic") return;

        entity.AcceptInput("Kill");
    }
}
