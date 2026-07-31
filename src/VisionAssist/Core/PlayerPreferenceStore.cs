using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace VisionAssist;

/// <summary>Per-player settings. Model colours are server-wide; these are not.</summary>
public sealed class PlayerPreferences
{
    [JsonPropertyName("hud")]
    public bool HudEnabled { get; set; } = true;

    /// <summary>s, m or l - mapped to the Panorama fontSize classes.</summary>
    [JsonPropertyName("hudSize")]
    public string HudSize { get; set; } = "l";

    [JsonPropertyName("lang")]
    public string Language { get; set; } = "uz";
}

/// <summary>
/// Keeps player preferences in a JSON file next to the plugin, keyed by SteamID64.
/// Bots and unauthorised connections get a throwaway object that is never saved.
/// </summary>
public sealed class PlayerPreferenceStore
{
    private readonly string _path;
    private readonly VisionAssistConfig _config;
    private readonly Action<string, Exception> _onError;

    private Dictionary<string, PlayerPreferences> _entries = new();
    private bool _dirty;

    public PlayerPreferenceStore(string directory, VisionAssistConfig config, Action<string, Exception> onError)
    {
        _path = Path.Combine(directory, "player_prefs.json");
        _config = config;
        _onError = onError;
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;

            var json = File.ReadAllText(_path);
            _entries = JsonSerializer.Deserialize<Dictionary<string, PlayerPreferences>>(json) ?? new();
        }
        catch (Exception ex)
        {
            _onError($"Could not read {_path}, starting with empty preferences", ex);
            _entries = new();
        }
    }

    public void Flush()
    {
        if (!_dirty) return;

        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
            _dirty = false;
        }
        catch (Exception ex)
        {
            _onError($"Could not write {_path}", ex);
        }
    }

    public PlayerPreferences Get(CCSPlayerController? player)
    {
        var steamId = player?.AuthorizedSteamID?.SteamId64;

        if (steamId is null or 0)
        {
            return new PlayerPreferences
            {
                HudSize = _config.HudDefaultSize,
                Language = _config.DefaultLanguage,
            };
        }

        var key = steamId.Value.ToString();

        if (!_entries.TryGetValue(key, out var prefs))
        {
            prefs = new PlayerPreferences
            {
                HudSize = _config.HudDefaultSize,
                Language = _config.DefaultLanguage,
            };

            _entries[key] = prefs;
            _dirty = true;
        }

        return prefs;
    }

    /// <summary>Call after changing anything on a <see cref="PlayerPreferences"/> instance.</summary>
    public void MarkDirty() => _dirty = true;

    public string LanguageOf(CCSPlayerController? player) => Get(player).Language;
}
