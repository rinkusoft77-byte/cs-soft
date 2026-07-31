namespace VisionAssist.Companion;

/// <summary>
/// Copies the two .cfg templates into the game's config folder, filling in the
/// port and token from companion.json.
/// </summary>
public sealed class CfgInstaller
{
    /// <summary>Read by the game on startup; wires up the GSI endpoint.</summary>
    public const string GsiFileName = "gamestate_integration_visionassist.cfg";

    /// <summary>Optional readability convars; the player execs this one.</summary>
    public const string AccessibilityFileName = "visionassist_accessibility.cfg";

    private readonly string _templateFolder;
    private readonly CompanionConfig _config;

    public CfgInstaller(CompanionConfig config)
    {
        _config = config;
        _templateFolder = Path.Combine(AppContext.BaseDirectory, "cfg");
    }

    public string TemplateFolder => _templateFolder;

    public sealed record Result(string Path, bool Overwrote);

    /// <summary>
    /// Writes both files, returning what happened to each. Throws when a
    /// template is missing or the target folder cannot be written.
    /// </summary>
    public IReadOnlyList<Result> Install(string cfgFolder)
    {
        if (!Directory.Exists(cfgFolder))
            throw new DirectoryNotFoundException($"no such folder: {cfgFolder}");

        var results = new List<Result>();

        foreach (string name in new[] { GsiFileName, AccessibilityFileName })
        {
            string source = Path.Combine(_templateFolder, name);
            if (!File.Exists(source))
                throw new FileNotFoundException($"template missing: {source}", source);

            string content = File.ReadAllText(source)
                .Replace("__PORT__", _config.Port.ToString())
                .Replace("__TOKEN__", _config.Token);

            string target = Path.Combine(cfgFolder, name);
            bool existed = File.Exists(target);

            File.WriteAllText(target, content);
            results.Add(new Result(target, existed));
        }

        return results;
    }

    /// <summary>
    /// The GSI cfg with placeholders filled in, for printing when the folder
    /// cannot be found and the player has to place the file by hand.
    /// </summary>
    public string RenderGsiCfg()
    {
        string source = Path.Combine(_templateFolder, GsiFileName);
        if (!File.Exists(source)) return string.Empty;

        return File.ReadAllText(source)
            .Replace("__PORT__", _config.Port.ToString())
            .Replace("__TOKEN__", _config.Token);
    }
}
