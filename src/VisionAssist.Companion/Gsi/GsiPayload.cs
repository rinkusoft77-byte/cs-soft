using System.Text.Json.Serialization;

namespace VisionAssist.Companion;

/// <summary>
/// The JSON the game POSTs to the endpoint named in
/// gamestate_integration_visionassist.cfg.
///
/// Only the blocks that cfg asks for are modelled. Everything here describes
/// the local player's own client state - the same numbers already drawn on the
/// player's own HUD. The <c>allplayers_*</c> blocks, which the game only fills
/// in for spectators and GOTV, are deliberately not requested and not modelled.
/// </summary>
public sealed class GsiPayload
{
    [JsonPropertyName("provider")]
    public GsiProvider? Provider { get; set; }

    [JsonPropertyName("map")]
    public GsiMap? Map { get; set; }

    [JsonPropertyName("round")]
    public GsiRound? Round { get; set; }

    [JsonPropertyName("player")]
    public GsiPlayer? Player { get; set; }

    [JsonPropertyName("phase_countdowns")]
    public GsiPhaseCountdowns? PhaseCountdowns { get; set; }

    [JsonPropertyName("auth")]
    public GsiAuth? Auth { get; set; }
}

public sealed class GsiAuth
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

public sealed class GsiProvider
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("version")]
    public long Version { get; set; }

    /// <summary>SteamID64 of the player running the game.</summary>
    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public sealed class GsiMap
{
    /// <summary>competitive, casual, deathmatch, custom...</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>warmup, live, intermission, gameover.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    [JsonPropertyName("round")]
    public int Round { get; set; }

    [JsonPropertyName("team_ct")]
    public GsiTeam? TeamCt { get; set; }

    [JsonPropertyName("team_t")]
    public GsiTeam? TeamT { get; set; }
}

public sealed class GsiTeam
{
    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("consecutive_round_losses")]
    public int ConsecutiveRoundLosses { get; set; }

    [JsonPropertyName("timeouts_remaining")]
    public int TimeoutsRemaining { get; set; }
}

public sealed class GsiRound
{
    /// <summary>freezetime, live, over.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>planted, defused, exploded - absent while the bomb is carried.</summary>
    [JsonPropertyName("bomb")]
    public string? Bomb { get; set; }

    [JsonPropertyName("win_team")]
    public string? WinTeam { get; set; }
}

/// <summary>
/// From the <c>phase_countdowns</c> block: which clock is running and how long
/// is left on it. This is where the bomb fuse comes from.
/// </summary>
public sealed class GsiPhaseCountdowns
{
    /// <summary>freezetime, live, bomb, defuse, over, warmup, paused.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>Seconds, sent as a string such as "12.345".</summary>
    [JsonPropertyName("phase_ends_in")]
    public string? PhaseEndsIn { get; set; }
}

public sealed class GsiPlayer
{
    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>T, CT, or absent while not on a team.</summary>
    [JsonPropertyName("team")]
    public string? Team { get; set; }

    /// <summary>playing, menu, textinput.</summary>
    [JsonPropertyName("activity")]
    public string? Activity { get; set; }

    [JsonPropertyName("state")]
    public GsiPlayerState? State { get; set; }

    [JsonPropertyName("match_stats")]
    public GsiMatchStats? MatchStats { get; set; }

    [JsonPropertyName("weapons")]
    public Dictionary<string, GsiWeapon>? Weapons { get; set; }
}

public sealed class GsiPlayerState
{
    [JsonPropertyName("health")]
    public int Health { get; set; }

    [JsonPropertyName("armor")]
    public int Armor { get; set; }

    [JsonPropertyName("helmet")]
    public bool Helmet { get; set; }

    /// <summary>0-255, how much of the screen is still white.</summary>
    [JsonPropertyName("flashed")]
    public int Flashed { get; set; }

    [JsonPropertyName("smoked")]
    public int Smoked { get; set; }

    [JsonPropertyName("burning")]
    public int Burning { get; set; }

    [JsonPropertyName("money")]
    public int Money { get; set; }

    [JsonPropertyName("round_kills")]
    public int RoundKills { get; set; }

    [JsonPropertyName("round_killhs")]
    public int RoundHeadshotKills { get; set; }

    [JsonPropertyName("equip_value")]
    public int EquipmentValue { get; set; }

    [JsonPropertyName("defusekit")]
    public bool DefuseKit { get; set; }
}

public sealed class GsiMatchStats
{
    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("mvps")]
    public int Mvps { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }
}

public sealed class GsiWeapon
{
    /// <summary>weapon_ak47, weapon_knife_t, weapon_hegrenade...</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Rifle, Pistol, Knife, Grenade, C4...</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>active, holstered, reloading.</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("ammo_clip")]
    public int? AmmoClip { get; set; }

    [JsonPropertyName("ammo_clip_max")]
    public int? AmmoClipMax { get; set; }

    [JsonPropertyName("ammo_reserve")]
    public int? AmmoReserve { get; set; }
}
