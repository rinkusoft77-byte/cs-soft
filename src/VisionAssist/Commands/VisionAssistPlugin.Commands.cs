using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace VisionAssist;

public partial class VisionAssistPlugin
{
    // ------------------------------------------------------- player commands

    [ConsoleCommand("css_vision", "Opens the visibility settings menu")]
    [ConsoleCommand("css_vis", "Opens the visibility settings menu")]
    public void OnVisionCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid)
        {
            PrintStatus(null);
            return;
        }

        OpenMainMenu(player);
    }

    [ConsoleCommand("css_hud", "Turns your large HUD readout on or off")]
    [CommandHelper(minArgs: 0, usage: "[on|off]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHudCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid) return;

        var prefs = _prefs.Get(player);

        if (command.ArgCount < 2)
        {
            prefs.HudEnabled = !prefs.HudEnabled;
        }
        else if (TryParseToggle(player, command.GetArg(1), out var enabled))
        {
            prefs.HudEnabled = enabled;
        }
        else
        {
            return;
        }

        _prefs.MarkDirty();
        Reply(player, T(player, "toggled", T(player, "hud"), Bare(player, prefs.HudEnabled)));
    }

    [ConsoleCommand("css_hudsize", "Sets your HUD text size")]
    [CommandHelper(minArgs: 1, usage: "<s|m|l>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHudSizeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid) return;

        var size = command.GetArg(1).Trim().ToLowerInvariant();
        if (size is not ("s" or "m" or "l"))
        {
            Reply(player, T(player, "hud_size_bad"));
            return;
        }

        var prefs = _prefs.Get(player);
        prefs.HudSize = size;
        _prefs.MarkDirty();

        Reply(player, T(player, "hud_size_set", size));
    }

    [ConsoleCommand("css_lang", "Sets your language")]
    [CommandHelper(minArgs: 1, usage: "<uz|ru|en>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLangCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid) return;

        var language = command.GetArg(1).Trim().ToLowerInvariant();
        if (!Lang.IsSupported(language))
        {
            Reply(player, T(player, "lang_unknown", language, Lang.SupportedList()));
            return;
        }

        var prefs = _prefs.Get(player);
        prefs.Language = language;
        _prefs.MarkDirty();

        Reply(player, T(player, "lang_set"));
    }

    [ConsoleCommand("css_bigradar", "Applies the recommended radar settings to your client")]
    public void OnBigRadarCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid) return;

        ClientTips.ApplyRadar(player);
        Reply(player, T(player, "cfg_applied"));
    }

    [ConsoleCommand("css_cfg", "Prints the recommended client settings to your console")]
    public void OnCfgCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid) return;

        ClientTips.PrintAll(player);
        Reply(player, T(player, "cfg_sent"));
    }

    [ConsoleCommand("css_colors", "Lists the built-in colour names")]
    public void OnColorsCommand(CCSPlayerController? player, CommandInfo command)
    {
        Reply(player, T(player, "colors_list", ColorParser.NamesList()));
        Reply(player, T(player, "colors_hint"));
    }

    [ConsoleCommand("css_themes", "Lists the colour themes")]
    public void OnThemesCommand(CCSPlayerController? player, CommandInfo command)
    {
        Reply(player, T(player, "themes_list"));

        foreach (var theme in Theme.All)
        {
            var marker = string.Equals(theme.Key, Config.DefaultTheme, StringComparison.OrdinalIgnoreCase)
                ? "{lime}>{default} "
                : "  ";

            Reply(player, $"{marker}{{lime}}{theme.Key}{{default}} - {theme.Description}");
        }
    }

    [ConsoleCommand("css_vision_status", "Shows every current setting")]
    public void OnStatusCommand(CCSPlayerController? player, CommandInfo command) => PrintStatus(player);

    // -------------------------------------------------------- admin commands

    [ConsoleCommand("css_theme", "Applies a colour theme")]
    [CommandHelper(minArgs: 1, usage: "<theme>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnThemeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        var requested = command.GetArg(1);
        var theme = Theme.Find(requested);

        if (theme is null)
        {
            Reply(player, T(player, "theme_unknown", requested, Theme.KeyList()));
            return;
        }

        Config.DefaultTheme = theme.Key;
        ApplyConfiguredTheme();
        SaveConfig();
        RefreshColors();

        Broadcast("theme_set", theme.Key, theme.Description);
    }

    [ConsoleCommand("css_tint", "Turns model recolouring on or off")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTintCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.TintEnabled = enabled;
        SaveConfig();

        if (enabled) _tint.ApplyToAll();
        else _tint.ResetAll();

        BroadcastToggle("model_color", enabled);
    }

    [ConsoleCommand("css_tintcolor", "Sets the model colour")]
    [CommandHelper(minArgs: 2, usage: "<t|ct|all> <colour>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTintColorCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseColorArgs(player, command, out var team, out var hex)) return;

        if (team is "t" or "all") Config.TintColorT = hex;
        if (team is "ct" or "all") Config.TintColorCT = hex;

        Config.DefaultTheme = "custom";
        SaveConfig();
        _tint.ApplyToAll();

        Broadcast("color_set", Lang.Get(Config.DefaultLanguage, "model_color"), team.ToUpperInvariant(), hex);
    }

    [ConsoleCommand("css_tintalpha", "Sets how solid the model colour is (1-255)")]
    [CommandHelper(minArgs: 1, usage: "<1-255>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTintAlphaCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseInt(player, command.GetArg(1), out var alpha)) return;

        Config.TintAlpha = Math.Clamp(alpha, 1, 255);
        SaveConfig();
        _tint.ApplyToAll();

        Reply(player, T(player, "value_set", "alpha", Config.TintAlpha));
    }

    [ConsoleCommand("css_tinthealth", "Shades the model colour by remaining health")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTintHealthCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.TintFollowsHealth = enabled;
        SaveConfig();
        _tint.ApplyToAll();

        Reply(player, T(player, "value_set", "health shading", Bare(player, enabled)));
    }

    [ConsoleCommand("css_outline", "Turns the model outline on or off")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnOutlineCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.OutlineEnabled = enabled;
        SaveConfig();

        if (enabled) _outline.ApplyToAll();
        else _outline.ClearAll();

        BroadcastToggle("outline", enabled);
    }

    [ConsoleCommand("css_outlinecolor", "Sets the outline colour")]
    [CommandHelper(minArgs: 2, usage: "<t|ct|all> <colour>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnOutlineColorCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseColorArgs(player, command, out var team, out var hex)) return;

        if (team is "t" or "all") Config.OutlineColorT = hex;
        if (team is "ct" or "all") Config.OutlineColorCT = hex;

        Config.DefaultTheme = "custom";
        SaveConfig();
        _outline.RefreshColors();

        Broadcast("color_set", Lang.Get(Config.DefaultLanguage, "outline"), team.ToUpperInvariant(), hex);
    }

    [ConsoleCommand("css_outlinerange", "Sets how far away the outline still draws")]
    [CommandHelper(minArgs: 1, usage: "<units>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnOutlineRangeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseInt(player, command.GetArg(1), out var range)) return;

        Config.OutlineRange = Math.Max(0, range);
        SaveConfig();
        _outline.RefreshColors();

        Reply(player, T(player, "value_set", T(player, "outline"), Config.OutlineRange));
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

        BroadcastToggle("radar", enabled);
    }

    [ConsoleCommand("css_highlight", "Turns the bomb/hostage highlight on or off")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnHighlightCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.HighlightEnabled = enabled;
        SaveConfig();

        if (enabled) _highlight.Tick();
        else _highlight.ResetAll();

        BroadcastToggle("highlight", enabled);
    }

    [ConsoleCommand("css_serverhud", "Turns the large HUD on or off for the whole server")]
    [CommandHelper(minArgs: 1, usage: "<on|off>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnServerHudCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;
        if (!TryParseToggle(player, command.GetArg(1), out var enabled)) return;

        Config.HudEnabled = enabled;
        SaveConfig();

        BroadcastToggle("hud", enabled);
    }

    [ConsoleCommand("css_vision_reload", "Reloads VisionAssist.json from disk")]
    public void OnReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!HasAccess(player)) return;

        if (!TryLoadConfigFromDisk(out var error))
        {
            Reply(player, T(player, "reload_failed", error));
            return;
        }

        StartTimers();
        RefreshAllPlayers();

        Reply(player, T(player, "reloaded"));
    }

    // --------------------------------------------------------------- helpers

    private void RefreshColors()
    {
        _tint.ApplyToAll();
        _outline.RefreshColors();
    }

    private void PrintStatus(CCSPlayerController? player)
    {
        Reply(player, $"{{gold}}VisionAssist {ModuleVersion}{{default}} - {T(player, "status_title")}");
        Reply(player, $"  {T(player, "model_color")}: {OnOffText(player, Config.TintEnabled)} " +
                      $"T {Config.TintColorT} / CT {Config.TintColorCT} (alpha {Config.TintAlpha})");
        Reply(player, $"  {T(player, "outline")}: {OnOffText(player, Config.OutlineEnabled)} " +
                      $"T {Config.OutlineColorT} / CT {Config.OutlineColorCT} (range {Config.OutlineRange})");
        Reply(player, $"  {T(player, "radar")}: {OnOffText(player, Config.RadarEnabled)}");
        Reply(player, $"  {T(player, "highlight")}: {OnOffText(player, Config.HighlightEnabled)}");
        Reply(player, $"  {T(player, "hud")}: {OnOffText(player, Config.HudEnabled)}");
        Reply(player, $"  Theme: {{lime}}{Config.DefaultTheme}{{default}}");
    }

    private void BroadcastToggle(string featureKey, bool enabled)
    {
        foreach (var target in Utilities.GetPlayers())
        {
            if (!target.IsValid || target.IsBot || target.IsHLTV) continue;

            var lang = _prefs.LanguageOf(target);
            Chat.Reply(target, Config.ChatPrefix, Lang.Get(lang, "toggled",
                Lang.Get(lang, featureKey), Lang.Get(lang, enabled ? "on" : "off")));
        }

        Server.PrintToConsole($"[VisionAssist] {Lang.Get("en", featureKey)}: {(enabled ? "on" : "off")}");
    }

    private string Bare(CCSPlayerController? player, bool value) => T(player, value ? "on" : "off");

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
                Reply(player, T(player, "bad_toggle", value));
                enabled = false;
                return false;
        }
    }

    private bool TryParseInt(CCSPlayerController? player, string value, out int result)
    {
        if (int.TryParse(value.Trim(), out result)) return true;

        Reply(player, T(player, "bad_number", value));
        return false;
    }

    private bool TryParseColorArgs(CCSPlayerController? player, CommandInfo command,
        out string team, out string hex)
    {
        team = command.GetArg(1).Trim().ToLowerInvariant();
        hex = string.Empty;

        if (team == "both") team = "all";

        if (team is not ("t" or "ct" or "all"))
        {
            Reply(player, T(player, "bad_team"));
            return false;
        }

        var input = command.GetArg(2);
        if (!ColorParser.TryParse(input, out var color))
        {
            Reply(player, T(player, "bad_color", input, ColorParser.NamesList()));
            return false;
        }

        hex = ColorParser.ToHex(color.Value);
        return true;
    }
}
