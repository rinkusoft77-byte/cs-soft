using System.Text.RegularExpressions;

namespace VisionAssist.Companion;

/// <summary>
/// Finds <c>...\Counter-Strike Global Offensive\game\csgo\cfg</c>.
///
/// Deliberately filesystem-only, no registry: reading HKCU on a net8.0 target
/// drags in a platform-specific API for something a handful of probes and one
/// libraryfolders.vdf parse handle just as well - and it keeps this class
/// testable on the machine the repo is edited on.
/// </summary>
public static class SteamLocator
{
    private const string GameFolder = "Counter-Strike Global Offensive";

    /// <summary>Relative path from a Steam library root to the config folder.</summary>
    private static readonly string CfgUnderLibrary =
        Path.Combine("steamapps", "common", GameFolder, "game", "csgo", "cfg");

    /// <summary>
    /// Returns every config folder that actually exists, most likely first.
    /// Empty means the game was not found and the path has to be given with
    /// <c>--cs2-cfg</c>.
    /// </summary>
    public static IReadOnlyList<string> FindCfgFolders()
    {
        var found = new List<string>();

        foreach (string library in SteamLibraries())
        {
            string cfg = Path.Combine(library, CfgUnderLibrary);
            if (Directory.Exists(cfg) && !found.Contains(cfg, StringComparer.OrdinalIgnoreCase))
                found.Add(cfg);
        }

        return found;
    }

    /// <summary>Steam roots plus every extra library registered in them.</summary>
    private static IEnumerable<string> SteamLibraries()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in SteamRoots())
        {
            if (!Directory.Exists(root)) continue;
            if (seen.Add(root)) yield return root;

            foreach (string extra in ReadLibraryFolders(root))
            {
                if (Directory.Exists(extra) && seen.Add(extra)) yield return extra;
            }
        }
    }

    private static IEnumerable<string> SteamRoots()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("STEAM_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) yield return fromEnvironment;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
        {
            foreach (var folder in new[]
            {
                Environment.SpecialFolder.ProgramFilesX86,
                Environment.SpecialFolder.ProgramFiles,
            })
            {
                string basePath = Environment.GetFolderPath(folder);
                if (basePath.Length > 0) yield return Path.Combine(basePath, "Steam");
            }

            // Steam is very often moved off the system drive to save space.
            foreach (string drive in new[] { "C", "D", "E", "F" })
            {
                yield return $@"{drive}:\Steam";
                yield return $@"{drive}:\SteamLibrary";
                yield return $@"{drive}:\Games\Steam";
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return Path.Combine(home, "Library", "Application Support", "Steam");
        }
        else
        {
            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".steam", "root");
            yield return Path.Combine(home, ".local", "share", "Steam");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam",
                ".local", "share", "Steam");
        }
    }

    /// <summary>
    /// Pulls the "path" values out of steamapps/libraryfolders.vdf. Full VDF
    /// parsing is overkill - the file's only interesting lines look like
    /// <c>"path"    "D:\\SteamLibrary"</c>.
    /// </summary>
    private static IReadOnlyList<string> ReadLibraryFolders(string steamRoot)
    {
        string? text = TryRead(Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"));
        if (text is null) return Array.Empty<string>();

        var paths = new List<string>();
        foreach (Match match in Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\"",
                     RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2)))
        {
            // VDF escapes backslashes.
            paths.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
        }

        return paths;
    }

    private static string? TryRead(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
