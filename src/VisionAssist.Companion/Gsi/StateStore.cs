using System.Globalization;

namespace VisionAssist.Companion;

/// <summary>
/// Holds the newest <see cref="OverlaySnapshot"/> and turns raw GSI payloads
/// into it. A single reference swap is all the locking that is needed: readers
/// only ever want the latest complete snapshot.
/// </summary>
public sealed class StateStore
{
    /// <summary>
    /// The game sends a heartbeat every 10 s (see the cfg), so nothing for 25 s
    /// means the game is closed or the cfg is not loaded.
    /// </summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(25);

    private OverlaySnapshot _snapshot = OverlaySnapshot.Waiting;

    /// <summary>
    /// Ticks rather than a DateTime: the watchdog reads this from another thread,
    /// and a long can be read atomically where a 16-byte struct cannot.
    /// </summary>
    private long _lastPayloadTicks;

    public OverlaySnapshot Current => Volatile.Read(ref _snapshot);

    /// <summary>True once a payload has ever arrived and the last one is recent.</summary>
    public bool IsFresh
    {
        get
        {
            long ticks = Interlocked.Read(ref _lastPayloadTicks);
            return ticks != 0 && DateTime.UtcNow.Ticks - ticks < StaleAfter.Ticks;
        }
    }

    public OverlaySnapshot Apply(GsiPayload payload)
    {
        Interlocked.Exchange(ref _lastPayloadTicks, DateTime.UtcNow.Ticks);
        var snapshot = Build(payload);
        Volatile.Write(ref _snapshot, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Rewrites the current snapshot as disconnected. Called by the staleness
    /// watchdog so the page can grey itself out when the game goes away.
    /// </summary>
    public OverlaySnapshot MarkDisconnected()
    {
        var snapshot = Current with
        {
            Connected = false,
            InGame = false,
            Notice = "game-not-running",
        };
        Volatile.Write(ref _snapshot, snapshot);
        return snapshot;
    }

    private static OverlaySnapshot Build(GsiPayload payload)
    {
        var player = payload.Player;
        var state = player?.State;

        // "activity" is menu while the scoreboard or the buy menu has focus, so
        // it is not a reliable in-game test on its own; the presence of a state
        // block with a team is.
        bool inGame = state is not null && !string.IsNullOrEmpty(player?.Team);

        var (active, grenades, hasBomb) = ReadWeapons(player?.Weapons);

        return new OverlaySnapshot
        {
            Connected = true,
            InGame = inGame,
            Notice = inGame ? null : "spectating-or-menu",

            MapName = payload.Map?.Name,
            MapMode = payload.Map?.Mode,
            MapPhase = payload.Map?.Phase,
            RoundNumber = payload.Map?.Round ?? 0,
            ScoreCt = payload.Map?.TeamCt?.Score ?? 0,
            ScoreT = payload.Map?.TeamT?.Score ?? 0,

            RoundPhase = payload.Round?.Phase,
            BombState = payload.Round?.Bomb,
            CountdownPhase = payload.PhaseCountdowns?.Phase,
            CountdownSeconds = ParseSeconds(payload.PhaseCountdowns?.PhaseEndsIn),

            PlayerName = player?.Name,
            Team = player?.Team,
            Health = state?.Health ?? 0,
            Armor = state?.Armor ?? 0,
            Helmet = state?.Helmet ?? false,
            DefuseKit = state?.DefuseKit ?? false,
            Money = state?.Money ?? 0,
            EquipmentValue = state?.EquipmentValue ?? 0,

            Flashed = state?.Flashed ?? 0,
            Smoked = state?.Smoked ?? 0,
            Burning = state?.Burning ?? 0,

            WeaponName = WeaponNames.Pretty(active?.Name),
            WeaponType = active?.Type,
            Reloading = string.Equals(active?.State, "reloading", StringComparison.OrdinalIgnoreCase),
            AmmoClip = active?.AmmoClip,
            AmmoClipMax = active?.AmmoClipMax,
            AmmoReserve = active?.AmmoReserve,
            Grenades = grenades,
            HasBomb = hasBomb,

            RoundKills = state?.RoundKills ?? 0,
            Kills = player?.MatchStats?.Kills ?? 0,
            Assists = player?.MatchStats?.Assists ?? 0,
            Deaths = player?.MatchStats?.Deaths ?? 0,
            Mvps = player?.MatchStats?.Mvps ?? 0,
        };
    }

    /// <summary>
    /// Picks out the weapon currently in hand plus the utility worth showing.
    /// The payload keys are weapon_0, weapon_1, ... in inventory order, which
    /// says nothing about which one is drawn - only "state" does.
    /// </summary>
    private static (GsiWeapon? Active, List<string> Grenades, bool HasBomb) ReadWeapons(
        Dictionary<string, GsiWeapon>? weapons)
    {
        var grenades = new List<string>();
        bool hasBomb = false;
        GsiWeapon? active = null;

        if (weapons is null) return (null, grenades, false);

        foreach (var weapon in weapons.Values)
        {
            bool isActive = string.Equals(weapon.State, "active", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(weapon.State, "reloading", StringComparison.OrdinalIgnoreCase);
            if (isActive) active = weapon;

            if (string.Equals(weapon.Type, "Grenade", StringComparison.OrdinalIgnoreCase))
            {
                string name = WeaponNames.Pretty(weapon.Name);
                if (name.Length > 0) grenades.Add(name);
            }
            else if (string.Equals(weapon.Type, "C4", StringComparison.OrdinalIgnoreCase))
            {
                hasBomb = true;
            }
        }

        return (active, grenades, hasBomb);
    }

    private static double? ParseSeconds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            ? Math.Max(0, seconds)
            : null;
    }
}
