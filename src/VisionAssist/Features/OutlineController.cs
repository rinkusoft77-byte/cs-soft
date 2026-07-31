using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VisionAssist;

/// <summary>
/// Draws a coloured edge around the player model, which makes the silhouette
/// readable when the model and the background are close in brightness.
///
/// CS2 has no "outline this player" server input, so two prop_dynamic copies of
/// the player's own model are spawned and parented to them:
///   * relay - render mode none, follows the pawn. A glow prop cannot follow a
///     player pawn directly without inheriting animation problems, so this sits
///     in between.
///   * glow  - follows the relay and carries the CGlowProperty fields.
///
/// The glow type is fixed to the non-through-walls mode. There is no toggle for
/// the see-through-walls mode, which is what separates an outline from an X-ray.
/// </summary>
public sealed class OutlineController
{
    private readonly VisionAssistPlugin _plugin;

    // playerSlot -> the two helper entities spawned for that player.
    private readonly Dictionary<int, OutlineEntities> _active = new();

    public OutlineController(VisionAssistPlugin plugin) => _plugin = plugin;

    private VisionAssistConfig Config => _plugin.Config;

    private readonly record struct OutlineEntities(uint RelayIndex, uint GlowIndex);

    /// <summary>
    /// Through-walls mode, refused on purpose. Anything the config asks for is
    /// clamped away from this value.
    /// </summary>
    private const int ThroughWallsGlowType = 3;

    private int GlowType
    {
        get
        {
            var configured = Config.OutlineGlowType;
            return configured == ThroughWallsGlowType || configured < 0 ? 2 : configured;
        }
    }

    public void ApplyToAll()
    {
        foreach (var player in Utilities.GetPlayers()) Apply(player);
    }

    public void Apply(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid || player.IsHLTV) return;

        Clear(player.Slot);

        if (!Config.OutlineEnabled || !player.PawnIsAlive) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return;

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
        // block movement or bullets.
        relay.Spawnflags = 256u;
        relay.RenderMode = RenderMode_t.kRenderNone;
        relay.SetModel(modelName);
        relay.DispatchSpawn();

        glow.Spawnflags = 256u;
        glow.SetModel(modelName);
        glow.DispatchSpawn();

        ApplyGlowProperties(glow, ColorFor(player.TeamNum));

        // FollowEntity with "!activator" makes the caller follow the activator.
        relay.AcceptInput("FollowEntity", pawn, relay, "!activator");
        glow.AcceptInput("FollowEntity", relay, glow, "!activator");

        _active[player.Slot] = new OutlineEntities(relay.Index, glow.Index);
    }

    private void ApplyGlowProperties(CDynamicProp glow, Color color)
    {
        glow.Glow.GlowColorOverride = color;
        glow.Glow.GlowTeam = -1;                                // both teams
        glow.Glow.GlowType = GlowType;
        glow.Glow.GlowRange = Math.Max(0, Config.OutlineRange);
        glow.Glow.GlowRangeMin = 0;
        glow.Glow.Glowing = true;

        Utilities.SetStateChanged(glow, "CBaseModelEntity", "m_Glow");
    }

    public Color ColorFor(int teamNum) => teamNum == (int)CsTeam.CounterTerrorist
        ? ColorParser.ParseOrDefault(Config.OutlineColorCT, Color.Cyan)
        : ColorParser.ParseOrDefault(Config.OutlineColorT, Color.Yellow);

    /// <summary>Pushes new colours onto existing props instead of respawning them.</summary>
    public void RefreshColors()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            if (!Config.OutlineEnabled)
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
            if (glow is null || !glow.IsValid || glow.DesignerName != "prop_dynamic")
            {
                Apply(player);
                continue;
            }

            ApplyGlowProperties(glow, ColorFor(player.TeamNum));
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
        foreach (var slot in _active.Keys.ToList()) Clear(slot);
    }

    /// <summary>
    /// Drops the bookkeeping without touching the entities. Used on round
    /// restart, where the engine already wiped every non-preserved entity and
    /// the indices we stored may since have been handed to something else.
    /// </summary>
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
