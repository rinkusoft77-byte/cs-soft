namespace VisionAssist.Companion;

/// <summary>
/// The flattened state the overlay page draws. Serialised with a camelCase
/// policy, so <c>RoundPhase</c> arrives in the browser as <c>roundPhase</c>.
/// </summary>
public sealed record OverlaySnapshot
{
    /// <summary>A GSI payload arrived recently enough to trust.</summary>
    public bool Connected { get; init; }

    /// <summary>The player is in a round rather than sitting in a menu.</summary>
    public bool InGame { get; init; }

    /// <summary>Set when the game has connected but is not playing yet.</summary>
    public string? Notice { get; init; }

    // --------------------------------------------------------------- the map

    public string? MapName { get; init; }
    public string? MapMode { get; init; }
    public string? MapPhase { get; init; }
    public int RoundNumber { get; init; }
    public int ScoreCt { get; init; }
    public int ScoreT { get; init; }

    // ------------------------------------------------------------- the round

    /// <summary>freezetime, live, over.</summary>
    public string? RoundPhase { get; init; }

    /// <summary>planted, defused, exploded, or null.</summary>
    public string? BombState { get; init; }

    /// <summary>Which clock <see cref="CountdownSeconds"/> belongs to.</summary>
    public string? CountdownPhase { get; init; }

    /// <summary>Seconds left on that clock when this snapshot was built.</summary>
    public double? CountdownSeconds { get; init; }

    // ------------------------------------------------------------ the player

    public string? PlayerName { get; init; }
    public string? Team { get; init; }
    public int Health { get; init; }
    public int Armor { get; init; }
    public bool Helmet { get; init; }
    public bool DefuseKit { get; init; }
    public int Money { get; init; }
    public int EquipmentValue { get; init; }

    /// <summary>0-255 each. Anything above zero is worth showing large.</summary>
    public int Flashed { get; init; }
    public int Smoked { get; init; }
    public int Burning { get; init; }

    // ------------------------------------------------------------ the weapon

    public string? WeaponName { get; init; }
    public string? WeaponType { get; init; }
    public bool Reloading { get; init; }
    public int? AmmoClip { get; init; }
    public int? AmmoClipMax { get; init; }
    public int? AmmoReserve { get; init; }

    /// <summary>Grenades in the inventory, already prettified for display.</summary>
    public IReadOnlyList<string> Grenades { get; init; } = Array.Empty<string>();

    public bool HasBomb { get; init; }

    // ------------------------------------------------------------ the scoring

    public int RoundKills { get; init; }
    public int Kills { get; init; }
    public int Assists { get; init; }
    public int Deaths { get; init; }
    public int Mvps { get; init; }

    /// <summary>Nothing has arrived from the game yet.</summary>
    public static readonly OverlaySnapshot Waiting = new()
    {
        Connected = false,
        InGame = false,
        Notice = "waiting-for-game",
    };
}
