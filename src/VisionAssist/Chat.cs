using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VisionAssist;

/// <summary>
/// Small chat helper so messages can be written as "{green}text{default}"
/// instead of string-concatenating ChatColors constants everywhere.
/// </summary>
public static class Chat
{
    private static readonly (string Tag, char Color)[] Tags =
    {
        ("{default}", ChatColors.Default),
        ("{white}", ChatColors.White),
        ("{darkred}", ChatColors.DarkRed),
        ("{green}", ChatColors.Green),
        ("{lightyellow}", ChatColors.LightYellow),
        ("{lightblue}", ChatColors.LightBlue),
        ("{olive}", ChatColors.Olive),
        ("{lime}", ChatColors.Lime),
        ("{red}", ChatColors.Red),
        ("{grey}", ChatColors.Grey),
        ("{yellow}", ChatColors.Yellow),
        ("{silver}", ChatColors.Silver),
        ("{blue}", ChatColors.Blue),
        ("{darkblue}", ChatColors.DarkBlue),
        ("{purple}", ChatColors.Magenta),
        ("{magenta}", ChatColors.Magenta),
        ("{gold}", ChatColors.Gold),
    };

    public static string Format(string message)
    {
        foreach (var (tag, color) in Tags)
        {
            message = message.Replace(tag, color.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return message;
    }

    /// <summary>Replies in chat when a player ran the command, in the console when the server did.</summary>
    public static void Reply(CCSPlayerController? player, string prefix, string message)
    {
        var formatted = Format($"{prefix} {message}");

        if (player is null || !player.IsValid)
        {
            Server.PrintToConsole(StripTags(formatted));
            return;
        }

        player.PrintToChat(formatted);
    }

    public static void Broadcast(string prefix, string message)
        => Server.PrintToChatAll(Format($"{prefix} {message}"));

    private static string StripTags(string message)
    {
        foreach (var (_, color) in Tags)
        {
            message = message.Replace(color.ToString(), string.Empty);
        }

        return message;
    }
}
