using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace VisionAssist;

public partial class VisionAssistPlugin
{
    /// <summary>
    /// Chat menu behind !vision. Player entries are always shown; the admin
    /// block only appears for people who hold the configured flag.
    /// </summary>
    private void OpenMainMenu(CCSPlayerController player)
    {
        var prefs = _prefs.Get(player);
        var menu = new ChatMenu(T(player, "menu_title"));

        menu.AddMenuOption(T(player, "menu_hud_toggle", Bare(player, prefs.HudEnabled)), (target, _) =>
        {
            var p = _prefs.Get(target);
            p.HudEnabled = !p.HudEnabled;
            _prefs.MarkDirty();

            Reply(target, T(target, "toggled", T(target, "hud"), Bare(target, p.HudEnabled)));
            OpenMainMenu(target);
        });

        menu.AddMenuOption(T(player, "menu_hud_size", prefs.HudSize), (target, _) =>
        {
            var p = _prefs.Get(target);
            p.HudSize = p.HudSize switch { "s" => "m", "m" => "l", _ => "s" };
            _prefs.MarkDirty();

            Reply(target, T(target, "hud_size_set", p.HudSize));
            OpenMainMenu(target);
        });

        menu.AddMenuOption(T(player, "menu_language"), (target, _) =>
        {
            var p = _prefs.Get(target);
            p.Language = p.Language switch { "uz" => "ru", "ru" => "en", _ => "uz" };
            _prefs.MarkDirty();

            Reply(target, T(target, "lang_set"));
            OpenMainMenu(target);
        });

        menu.AddMenuOption(T(player, "menu_client_cfg"), (target, _) =>
        {
            ClientTips.ApplyRadar(target);
            ClientTips.PrintAll(target);

            Reply(target, T(target, "cfg_applied"));
            Reply(target, T(target, "cfg_sent"));
        });

        menu.AddMenuOption(T(player, "menu_themes"), (target, _) =>
        {
            Reply(target, T(target, "themes_list"));

            foreach (var theme in Theme.All)
            {
                Reply(target, $"  {{lime}}{theme.Key}{{default}} - {theme.Description}");
            }
        });

        menu.AddMenuOption(T(player, "menu_status"), (target, _) => PrintStatus(target));

        if (AdminManager.PlayerHasPermissions(player, Config.AdminFlag))
        {
            AddAdminOptions(menu);
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void AddAdminOptions(ChatMenu menu)
    {
        menu.AddMenuOption("--- admin ---", (_, _) => { }, disabled: true);

        menu.AddMenuOption(Lang.Get(Config.DefaultLanguage, "menu_admin_theme"), (target, _) =>
        {
            if (!HasAccess(target)) return;

            // Walk the preset list so an admin can compare them in-game without
            // having to remember the names.
            var index = Theme.All.ToList().FindIndex(t =>
                string.Equals(t.Key, Config.DefaultTheme, StringComparison.OrdinalIgnoreCase));

            var next = Theme.All[(index + 1) % Theme.All.Count];

            Config.DefaultTheme = next.Key;
            ApplyConfiguredTheme();
            SaveConfig();
            RefreshColors();

            Broadcast("theme_set", next.Key, next.Description);
            OpenMainMenu(target);
        });

        menu.AddMenuOption(AdminToggleLabel("menu_admin_tint", Config.TintEnabled), (target, _) =>
        {
            if (!HasAccess(target)) return;

            Config.TintEnabled = !Config.TintEnabled;
            SaveConfig();

            if (Config.TintEnabled) _tint.ApplyToAll();
            else _tint.ResetAll();

            BroadcastToggle("model_color", Config.TintEnabled);
            OpenMainMenu(target);
        });

        menu.AddMenuOption(AdminToggleLabel("menu_admin_outline", Config.OutlineEnabled), (target, _) =>
        {
            if (!HasAccess(target)) return;

            Config.OutlineEnabled = !Config.OutlineEnabled;
            SaveConfig();

            if (Config.OutlineEnabled) _outline.ApplyToAll();
            else _outline.ClearAll();

            BroadcastToggle("outline", Config.OutlineEnabled);
            OpenMainMenu(target);
        });

        menu.AddMenuOption(AdminToggleLabel("menu_admin_radar", Config.RadarEnabled), (target, _) =>
        {
            if (!HasAccess(target)) return;

            Config.RadarEnabled = !Config.RadarEnabled;
            SaveConfig();

            if (!Config.RadarEnabled) _radar.Reset();

            BroadcastToggle("radar", Config.RadarEnabled);
            OpenMainMenu(target);
        });

        menu.AddMenuOption(AdminToggleLabel("menu_admin_highlight", Config.HighlightEnabled), (target, _) =>
        {
            if (!HasAccess(target)) return;

            Config.HighlightEnabled = !Config.HighlightEnabled;
            SaveConfig();

            if (Config.HighlightEnabled) _highlight.Tick();
            else _highlight.ResetAll();

            BroadcastToggle("highlight", Config.HighlightEnabled);
            OpenMainMenu(target);
        });
    }

    private string AdminToggleLabel(string key, bool enabled)
    {
        var lang = Config.DefaultLanguage;
        return Lang.Get(lang, key, Lang.Get(lang, enabled ? "on" : "off"));
    }
}
