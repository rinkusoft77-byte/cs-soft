using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VisionAssist;

/// <summary>
/// Keeps every player marked as "spotted" so all of them stay visible on the
/// radar. The engine recomputes the spotted state constantly, which is why this
/// runs on a repeating timer instead of once per round.
///
/// m_bSpottedByMask is a 64-bit mask of "who has spotted this entity", split
/// into two uints: index 0 covers player slots 0-31, index 1 covers 32-63.
/// Setting both to 0xFFFFFFFF means "spotted by everyone", which is what makes
/// the blip show up for all players rather than just one.
/// </summary>
public sealed class RadarController
{
    private readonly VisionAssistPlugin _plugin;

    public RadarController(VisionAssistPlugin plugin) => _plugin = plugin;

    private VisionAssistConfig Config => _plugin.Config;

    public void Tick()
    {
        if (!Config.RadarEnabled) return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsHLTV) continue;
            if (!player.PawnIsAlive && !Config.RadarIncludeDead) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn is null || !pawn.IsValid) continue;

            MarkSpotted(pawn);
        }
    }

    private static void MarkSpotted(CCSPlayerPawn pawn)
    {
        pawn.EntitySpottedState.Spotted = true;
        pawn.EntitySpottedState.SpottedByMask[0] = uint.MaxValue;
        pawn.EntitySpottedState.SpottedByMask[1] = uint.MaxValue;

        // m_bSpotted / m_bSpottedByMask live inside the embedded
        // EntitySpottedState_t, so the change is flagged on the field that
        // holds it (CCSPlayerPawn::m_entitySpottedState), not on the inner
        // members - those are not networked fields of the pawn itself.
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState");
    }

    /// <summary>Hands the spotted state back to the engine when the feature is turned off.</summary>
    public void Reset()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn is null || !pawn.IsValid) continue;

            pawn.EntitySpottedState.Spotted = false;
            pawn.EntitySpottedState.SpottedByMask[0] = 0;
            pawn.EntitySpottedState.SpottedByMask[1] = 0;

            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState");
        }
    }
}
