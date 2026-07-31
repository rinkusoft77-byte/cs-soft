using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

namespace VisionAssist;

/// <summary>
/// Visibility and readability plugin for a CS2 server you run yourself.
///
/// Everything is server-side and networked to every client, so all players see
/// the same thing. Two deliberate limits: the outline has no see-through-walls
/// mode, and nothing here is filtered per viewer - there is no way to make an
/// effect visible to one player only.
/// </summary>
[MinimumApiVersion(305)]
public partial class VisionAssistPlugin : BasePlugin, IPluginConfig<VisionAssistConfig>
{
    public override string ModuleName => "VisionAssist";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "rinkusoft77-byte";
    public override string ModuleDescription =>
        "Model recolouring, outlines, objective highlights, a large HUD and full radar for a self-hosted CS2 server.";

    public VisionAssistConfig Config { get; set; } = new();

    private PlayerPreferenceStore _prefs = null!;
    private TintController _tint = null!;
    private OutlineController _outline = null!;
    private RadarController _radar = null!;
    private HighlightController _highlight = null!;
    private HudController _hud = null!;

    private readonly List<CounterStrikeSharp.API.Modules.Timers.Timer> _timers = new();

    public void OnConfigParsed(VisionAssistConfig config)
    {
        // Keep the timers out of pathological territory.
        config.RadarUpdateInterval = Math.Clamp(config.RadarUpdateInterval, 0.1f, 5.0f);
        config.HudUpdateInterval = Math.Clamp(config.HudUpdateInterval, 0.1f, 2.0f);
        config.HighlightInterval = Math.Clamp(config.HighlightInterval, 0.25f, 10.0f);
        config.TintAlpha = Math.Clamp(config.TintAlpha, 1, 255);
        config.HudLowHealthThreshold = Math.Clamp(config.HudLowHealthThreshold, 0, 100);

        if (!Lang.IsSupported(config.DefaultLanguage)) config.DefaultLanguage = "en";

        Config = config;

        ApplyConfiguredTheme();
    }

    /// <summary>
    /// DefaultTheme drives the colours unless it is "custom". Setting a colour
    /// by hand switches it to "custom" so the individual values are respected.
    /// </summary>
    private void ApplyConfiguredTheme()
    {
        var theme = Theme.Find(Config.DefaultTheme);
        if (theme is null) return;

        Config.TintColorT = theme.ColorT;
        Config.TintColorCT = theme.ColorCT;
        Config.OutlineColorT = theme.ColorT;
        Config.OutlineColorCT = theme.ColorCT;
    }

    public override void Load(bool hotReload)
    {
        _prefs = new PlayerPreferenceStore(ModuleDirectory, Config,
            (message, ex) => Logger.LogError(ex, "{Message}", message));
        _prefs.Load();

        _tint = new TintController(this);
        _outline = new OutlineController(this);
        _radar = new RadarController(this);
        _highlight = new HighlightController(this);
        _hud = new HudController(this, _prefs);

        RegisterListener<Listeners.OnClientDisconnect>(slot =>
        {
            _outline.Clear(slot);
            _prefs.Flush();
        });

        RegisterListener<Listeners.OnMapStart>(_ => _outline.ForgetAll());

        StartTimers();

        if (hotReload) RefreshAllPlayers();

        Logger.LogInformation(
            "VisionAssist {Version} loaded (tint: {Tint}, outline: {Outline}, radar: {Radar}, HUD: {Hud}).",
            ModuleVersion, Config.TintEnabled, Config.OutlineEnabled, Config.RadarEnabled, Config.HudEnabled);
    }

    public override void Unload(bool hotReload)
    {
        StopTimers();

        _outline.ClearAll();
        _tint.ResetAll();
        _highlight.ResetAll();
        _radar.Reset();
        _prefs.Flush();
    }

    private void StartTimers()
    {
        StopTimers();

        _timers.Add(AddTimer(Config.RadarUpdateInterval, () => _radar.Tick(), TimerFlags.REPEAT));
        _timers.Add(AddTimer(Config.HudUpdateInterval, () => _hud.Tick(), TimerFlags.REPEAT));
        _timers.Add(AddTimer(Config.HighlightInterval, () => _highlight.Tick(), TimerFlags.REPEAT));
        _timers.Add(AddTimer(60.0f, () => _prefs.Flush(), TimerFlags.REPEAT));
    }

    private void StopTimers()
    {
        foreach (var timer in _timers) timer.Kill();
        _timers.Clear();
    }

    /// <summary>Reapplies model colour and outline to everyone currently in the server.</summary>
    private void RefreshAllPlayers()
    {
        _tint.ApplyToAll();
        _outline.ApplyToAll();
    }

    // ---------------------------------------------------------------- events

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !player.IsValid) return HookResult.Continue;

        // The pawn's model is not assigned yet on the spawn tick, and the
        // outline props are clones of that model, so wait a beat.
        AddTimer(0.2f, () =>
        {
            if (!player.IsValid) return;

            _tint.Apply(player);
            _outline.Apply(player);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is not null && player.IsValid) _outline.Clear(player.Slot);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (!Config.TintFollowsHealth) return HookResult.Continue;

        var player = @event.Userid;
        if (player is not null && player.IsValid && player.PawnIsAlive) _tint.Apply(player);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !player.IsValid) return HookResult.Continue;

        // Team decides the colour, so rebuild once the switch has settled.
        AddTimer(0.3f, () =>
        {
            if (!player.IsValid) return;

            _tint.Apply(player);
            _outline.Apply(player);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Round restart wipes non-preserved entities, our props included. Drop
        // the stale indices rather than killing whatever now occupies them.
        _outline.ForgetAll();

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (!Config.AnnounceOnJoin) return HookResult.Continue;

        var player = @event.Userid;
        if (player is null || !player.IsValid || player.IsBot) return HookResult.Continue;

        AddTimer(4.0f, () =>
        {
            if (player.IsValid) Reply(player, T(player, "announce"));
        });

        return HookResult.Continue;
    }

    // --------------------------------------------------------------- helpers

    private string T(CCSPlayerController? player, string key, params object[] args)
        => Lang.Get(_prefs.LanguageOf(player), key, args);

    private void Reply(CCSPlayerController? player, string message)
        => Chat.Reply(player, Config.ChatPrefix, message);

    private void Broadcast(string key, params object[] args)
    {
        // Broadcasts go out per player so each one reads it in their language.
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV) continue;

            Chat.Reply(player, Config.ChatPrefix, Lang.Get(_prefs.LanguageOf(player), key, args));
        }

        Server.PrintToConsole($"[VisionAssist] {Lang.Get("en", key, args)}");
    }

    private bool HasAccess(CCSPlayerController? player)
    {
        // Null player means the server console, which always has access.
        if (player is null || !player.IsValid) return true;
        if (AdminManager.PlayerHasPermissions(player, Config.AdminFlag)) return true;

        Reply(player, T(player, "no_access"));
        return false;
    }

    private string OnOffText(CCSPlayerController? player, bool value)
        => value ? $"{{lime}}{T(player, "on")}{{default}}" : $"{{red}}{T(player, "off")}{{default}}";

    private string ConfigPath => Path.GetFullPath(Path.Combine(
        ModuleDirectory, "..", "..", "configs", "plugins", ModuleName, $"{ModuleName}.json"));

    /// <summary>
    /// Persists live command changes so they survive a map change or restart.
    /// Failures are logged rather than thrown - a read-only config file should
    /// not take the plugin down mid-round.
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not write {Path}", ConfigPath);
        }
    }

    private bool TryLoadConfigFromDisk(out string error)
    {
        error = string.Empty;

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var parsed = JsonSerializer.Deserialize<VisionAssistConfig>(json);

            if (parsed is null)
            {
                error = "file is empty or invalid JSON";
                return false;
            }

            OnConfigParsed(parsed);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
