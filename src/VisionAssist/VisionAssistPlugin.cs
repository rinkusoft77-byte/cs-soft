using System.Text.Json;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;

namespace VisionAssist;

/// <summary>
/// Server-side visibility plugin for a CS2 server you own.
///
/// Everything here runs on the server and is networked to every connected
/// client, so all players see the same thing. It cannot be used on servers you
/// do not administer, and there is no per-viewer filtering anywhere in this
/// plugin — the effects are a property of the server, not an advantage for one
/// player.
/// </summary>
[MinimumApiVersion(305)]
public class VisionAssistPlugin : BasePlugin, IPluginConfig<VisionAssistConfig>
{
    public override string ModuleName => "VisionAssist";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "rinkusoft77-byte";
    public override string ModuleDescription =>
        "High-contrast player outlines, model tint and full-team radar for a self-hosted CS2 server.";

    public VisionAssistConfig Config { get; set; } = new();

    private GlowController _glow = null!;
    private RadarController _radar = null!;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _radarTimer;

    public void OnConfigParsed(VisionAssistConfig config)
    {
        // Guard against values that would make the server do pointless work.
        config.RadarUpdateInterval = Math.Clamp(config.RadarUpdateInterval, 0.1f, 5.0f);
        config.TintAlpha = Math.Clamp(config.TintAlpha, 1, 255);

        Config = config;
    }

    public override void Load(bool hotReload)
    {
        _glow = new GlowController(this);
        _radar = new RadarController(this);

        RegisterListener<Listeners.OnClientDisconnect>(slot => _glow.Clear(slot));
        RegisterListener<Listeners.OnMapStart>(_ => _glow.ForgetAll());

        StartRadarTimer();

        if (hotReload)
        {
            _glow.ApplyToAll();
        }

        Logger.LogInformation(
            "VisionAssist loaded. Glow: {Glow}, tint: {Tint}, radar: {Radar}.",
            Config.GlowEnabled, Config.TintEnabled, Config.RadarEnabled);
    }

    public override void Unload(bool hotReload)
    {
        _radarTimer?.Kill();
        _radarTimer = null;

        _glow.ClearAll();
        _radar.Reset();
    }

    private void StartRadarTimer()
    {
        _radarTimer?.Kill();
        _radarTimer = AddTimer(Config.RadarUpdateInterval, () => _radar.Tick(), TimerFlags.REPEAT);
    }

    // ---------------------------------------------------------------- events

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !player.IsValid) return HookResult.Continue;

        // The pawn's model is not assigned yet on the spawn tick, and the glow
        // props are clones of that model, so wait a beat before building them.
        AddTimer(0.2f, () =>
        {
            if (player.IsValid) _glow.Apply(player);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is not null && player.IsValid) _glow.Clear(player.Slot);

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
            if (player.IsValid) _glow.Apply(player);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Round restart wipes non-preserved entities, our props included. Drop
        // the stale indices rather than killing whatever now occupies them.
        _glow.ForgetAll();

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (!Config.AnnounceOnJoin) return HookResult.Continue;

        var player = @event.Userid;
        if (player is null || !player.IsValid || player.IsBot) return HookResult.Continue;

        AddTimer(3.0f, () =>
        {
            if (!player.IsValid) return;

            Chat.Reply(player, Config.ChatPrefix,
                "{lime}This server runs enhanced visibility{default}: player outlines and full radar are on for {lime}everyone{default}.");
        });

        return HookResult.Continue;
    }

    // -------------------------------------------------------------- commands

    [ConsoleCommand("css_vision", "Shows the current VisionAssist settings")]
    public void OnVisionCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        Reply(player, "{gold}VisionAssist{default} status:");
        Reply(player, $"  Glow: {OnOff(Config.GlowEnabled)}  (through walls: {OnOff(Config.GlowThroughWalls)}, range: {Config.GlowRange})");
        Reply(player, $"  Glow colours - T: {Config.GlowColorT}, CT: {Config.GlowColorCT}");
        Reply(player, $"  Tint: {OnOff(Config.TintEnabled)}  (alpha: {Config.TintAlpha})");
        Reply(player, $"  Tint colours - T: {Config.TintColorT}, CT: {Config.TintColorCT}");
        Reply(player, $"  Radar: {OnOff(Config.RadarEnabled)}  (every {Config.RadarUpdateInterval:0.00}s, dead shown: {OnOff(Config.RadarIncludeDead)})");
        Reply(player, "  Commands: {lime}!glow !glowcolor !tint !tintcolor !glowrange !walls !radar !colors{default}");
    }

    [ConsoleCommand("css_glow", "Turns the player outline on or off")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGlowCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.GlowEnabled = enabled;
        SaveConfig();

        if (enabled) _glow.ApplyToAll();
        else foreach (var target in Utilities.GetPlayers()) _glow.Clear(target.Slot);

        Broadcast($"Outline {OnOff(enabled)}.");
    }

    [ConsoleCommand("css_glowcolor", "Sets the outline colour")]
    [CommandHelper(minArgs: 2, usage: "<t|ct|all> <colour>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGlowColorCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        var team = command.GetArg(1).ToLowerInvariant();
        var input = command.GetArg(2);

        if (!ColorParser.TryParse(input, out var color))
        {
            Reply(player, $"{{red}}Unknown colour{{default}} '{input}'. Use #RRGGBB, r,g,b or: {ColorParser.NamesList()}");
            return;
        }

        var hex = ColorParser.ToHex(color.Value);

        switch (team)
        {
            case "t":
                Config.GlowColorT = hex;
                break;
            case "ct":
                Config.GlowColorCT = hex;
                break;
            case "all":
            case "both":
                Config.GlowColorT = hex;
                Config.GlowColorCT = hex;
                break;
            default:
                Reply(player, "{red}Team must be{default} t, ct or all.");
                return;
        }

        SaveConfig();
        _glow.RefreshColors();

        Broadcast($"Outline colour for {{lime}}{team.ToUpperInvariant()}{{default}} set to {{lime}}{hex}{{default}}.");
    }

    [ConsoleCommand("css_tint", "Turns the model recolour on or off")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTintCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.TintEnabled = enabled;
        SaveConfig();
        _glow.ApplyToAll();

        Broadcast($"Model colour {OnOff(enabled)}.");
    }

    [ConsoleCommand("css_tintcolor", "Sets the model colour")]
    [CommandHelper(minArgs: 2, usage: "<t|ct|all> <colour>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTintColorCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        var team = command.GetArg(1).ToLowerInvariant();
        var input = command.GetArg(2);

        if (!ColorParser.TryParse(input, out var color))
        {
            Reply(player, $"{{red}}Unknown colour{{default}} '{input}'. Use #RRGGBB, r,g,b or: {ColorParser.NamesList()}");
            return;
        }

        var hex = ColorParser.ToHex(color.Value);

        switch (team)
        {
            case "t":
                Config.TintColorT = hex;
                break;
            case "ct":
                Config.TintColorCT = hex;
                break;
            case "all":
            case "both":
                Config.TintColorT = hex;
                Config.TintColorCT = hex;
                break;
            default:
                Reply(player, "{red}Team must be{default} t, ct or all.");
                return;
        }

        SaveConfig();
        _glow.RefreshColors();

        Broadcast($"Model colour for {{lime}}{team.ToUpperInvariant()}{{default}} set to {{lime}}{hex}{{default}}.");
    }

    [ConsoleCommand("css_glowrange", "Sets how far away the outline still draws")]
    [CommandHelper(minArgs: 1, usage: "<units, e.g. 5000>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGlowRangeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        if (!int.TryParse(command.GetArg(1), out var range) || range < 0)
        {
            Reply(player, "{red}Range must be a positive number{default}, e.g. 5000.");
            return;
        }

        Config.GlowRange = range;
        SaveConfig();
        _glow.RefreshColors();

        Reply(player, $"Outline range set to {{lime}}{range}{{default}}.");
    }

    [ConsoleCommand("css_walls", "Whether the outline draws through geometry")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnWallsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.GlowThroughWalls = enabled;
        SaveConfig();
        _glow.ApplyToAll();

        Broadcast($"Outline through walls {OnOff(enabled)}.");
    }

    [ConsoleCommand("css_radar", "Turns the all-players radar on or off")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRadarCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.RadarEnabled = enabled;
        SaveConfig();

        if (!enabled) _radar.Reset();

        Broadcast($"Full radar {OnOff(enabled)}.");
    }

    [ConsoleCommand("css_colors", "Lists the built-in colour names")]
    public void OnColorsCommand(CCSPlayerController? player, CommandInfo command)
    {
        Reply(player, $"Colours: {{lime}}{ColorParser.NamesList()}{{default}}");
        Reply(player, "Or use a hex value like {lime}#FF2ED1{default} / an RGB triplet like {lime}255,46,209{default}.");
    }

    [ConsoleCommand("css_vision_reload", "Reloads VisionAssist.json from disk")]
    public void OnReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        if (!TryLoadConfigFromDisk(out var error))
        {
            Reply(player, $"{{red}}Reload failed{{default}}: {error}");
            return;
        }

        StartRadarTimer();
        _glow.ApplyToAll();

        Reply(player, "Config reloaded.");
    }

    // --------------------------------------------------------------- helpers

    private bool HasAccess(CCSPlayerController? player)
    {
        // Null player means the server console, which always has access.
        if (player is null || !player.IsValid) return true;
        if (AdminManager.PlayerHasPermissions(player, Config.AdminFlag)) return true;

        Reply(player, "{red}You do not have access to this command.{default}");
        return false;
    }

    private bool TryParseToggle(CCSPlayerController? player, string value, out bool enabled)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "on":
            case "true":
            case "yes":
                enabled = true;
                return true;
            case "0":
            case "off":
            case "false":
            case "no":
                enabled = false;
                return true;
            default:
                Reply(player, $"{{red}}Expected on or off{{default}}, got '{value}'.");
                enabled = false;
                return false;
        }
    }

    private void Reply(CCSPlayerController? player, string message)
        => Chat.Reply(player, Config.ChatPrefix, message);

    private void Broadcast(string message)
        => Chat.Broadcast(Config.ChatPrefix, message);

    private static string OnOff(bool value) => value ? "{lime}on{default}" : "{red}off{default}";

    private string ConfigPath => Path.GetFullPath(Path.Combine(
        ModuleDirectory, "..", "..", "configs", "plugins", ModuleName, $"{ModuleName}.json"));

    /// <summary>
    /// Persists live command changes so they survive a map change or restart.
    /// Failures are logged rather than thrown — a read-only config file should
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
