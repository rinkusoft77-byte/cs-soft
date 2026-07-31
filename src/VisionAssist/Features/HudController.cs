using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VisionAssist;

/// <summary>
/// A large centre-screen readout of the things the default HUD prints small and
/// in the corners: health, armour, round time, bomb time and how many players
/// are left alive. Every player can size it or switch it off for themselves.
/// </summary>
public sealed class HudController
{
    private readonly VisionAssistPlugin _plugin;
    private readonly PlayerPreferenceStore _prefs;

    public HudController(VisionAssistPlugin plugin, PlayerPreferenceStore prefs)
    {
        _plugin = plugin;
        _prefs = prefs;
    }

    private VisionAssistConfig Config => _plugin.Config;

    public void Tick()
    {
        if (!Config.HudEnabled) return;

        var rules = GameRules();
        var (aliveT, aliveCt) = CountAlive();

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV) continue;

            var prefs = _prefs.Get(player);
            if (!prefs.HudEnabled) continue;

            var html = Build(player, prefs, rules, aliveT, aliveCt);
            if (html.Length == 0) continue;

            player.PrintToCenterHtml(html);
        }
    }

    private string Build(CCSPlayerController player, PlayerPreferences prefs, CCSGameRules? rules,
        int aliveT, int aliveCt)
    {
        var lang = prefs.Language;
        var hudColor = Config.HudColor;
        var line1 = new List<string>();
        var line2 = new List<string>();

        var pawn = player.PlayerPawn.Value;
        var alive = player.PawnIsAlive && pawn is not null && pawn.IsValid;

        if (Config.HudShowHealth && alive)
        {
            var health = pawn!.Health;
            var color = health <= Config.HudLowHealthThreshold ? Config.HudLowHealthColor : hudColor;
            line1.Add($"<font color='{color}'>{Lang.Get(lang, "hud_hp")} {health}</font>");
        }

        if (Config.HudShowArmor && alive && pawn!.ArmorValue > 0)
        {
            line1.Add($"{Lang.Get(lang, "hud_armor")} {pawn.ArmorValue}");
        }

        if (Config.HudShowBombTimer && rules is not null && rules.BombPlanted)
        {
            var remaining = BombRemaining();
            if (remaining > 0)
            {
                line2.Add($"<font color='{Config.HudLowHealthColor}'>{Lang.Get(lang, "hud_bomb")} {remaining:0.0}</font>");
            }
        }
        else if (Config.HudShowRoundTimer && rules is not null)
        {
            line2.Add(rules.FreezePeriod
                ? Lang.Get(lang, "hud_freeze")
                : FormatClock(RoundRemaining(rules)));
        }

        if (Config.HudShowAliveCount)
        {
            line2.Add($"{Lang.Get(lang, "hud_alive")} {aliveT} - {aliveCt}");
        }

        var body = new StringBuilder();
        if (line1.Count > 0) body.Append(string.Join("&nbsp;&nbsp;", line1));
        if (line1.Count > 0 && line2.Count > 0) body.Append("<br>");
        if (line2.Count > 0) body.Append(string.Join("&nbsp;&nbsp;", line2));

        if (body.Length == 0) return string.Empty;

        return $"<font class='{FontClass(prefs.HudSize)}' color='{hudColor}'>{body}</font>";
    }

    /// <summary>Panorama only honours these preset classes, not arbitrary pixel sizes.</summary>
    private static string FontClass(string size) => size?.Trim().ToLowerInvariant() switch
    {
        "s" => "fontSize-s",
        "m" => "fontSize-m",
        _ => "fontSize-l",
    };

    private static float RoundRemaining(CCSGameRules rules)
        => rules.RoundStartTime + rules.RoundTime - Server.CurrentTime;

    private static float BombRemaining()
    {
        var bomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        if (bomb is null || !bomb.IsValid || bomb.BombDefused) return 0;

        return bomb.C4Blow - Server.CurrentTime;
    }

    private static string FormatClock(float seconds)
    {
        if (seconds < 0) seconds = 0;

        var total = (int)Math.Ceiling(seconds);
        return $"{total / 60}:{total % 60:00}";
    }

    private static (int T, int Ct) CountAlive()
    {
        var t = 0;
        var ct = 0;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            switch ((CsTeam)player.TeamNum)
            {
                case CsTeam.Terrorist:
                    t++;
                    break;
                case CsTeam.CounterTerrorist:
                    ct++;
                    break;
            }
        }

        return (t, ct);
    }

    private static CCSGameRules? GameRules()
        => Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()?.GameRules;
}
