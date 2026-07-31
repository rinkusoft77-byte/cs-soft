namespace VisionAssist.Companion;

/// <summary>
/// Command line switches. Everything here overrides <see cref="CompanionConfig"/>
/// for the current run only - nothing is written back to the JSON file.
/// </summary>
public sealed class CompanionOptions
{
    /// <summary>Write the .cfg files into the CS2 config folder and exit.</summary>
    public bool Install { get; private set; }

    /// <summary>Print where the CS2 config folder was found and exit.</summary>
    public bool Locate { get; private set; }

    /// <summary>Explicit path to <c>...\game\csgo\cfg</c>, when auto-detection misses.</summary>
    public string? Cs2CfgPath { get; private set; }

    /// <summary>Port for the local overlay server.</summary>
    public int? Port { get; private set; }

    /// <summary>Folder to serve the overlay page from, for editing it live.</summary>
    public string? WebRoot { get; private set; }

    /// <summary>Open the overlay in the default browser once the server is up.</summary>
    public bool? OpenBrowser { get; private set; }

    /// <summary>Log every accepted GSI payload, for working out why a field is empty.</summary>
    public bool Verbose { get; private set; }

    /// <summary>Skip the confirmation prompt in <c>--install</c>.</summary>
    public bool AssumeYes { get; private set; }

    public static CompanionOptions Parse(string[] args)
    {
        var options = new CompanionOptions();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--install":
                    options.Install = true;
                    break;

                case "--locate":
                    options.Locate = true;
                    break;

                case "--cs2-cfg":
                    options.Cs2CfgPath = NextValue(args, ref i, arg);
                    break;

                case "--port":
                    string port = NextValue(args, ref i, arg);
                    if (!int.TryParse(port, out int parsed) || parsed is < 1 or > 65535)
                        throw new ArgumentException($"--port needs a number between 1 and 65535, got '{port}'");
                    options.Port = parsed;
                    break;

                case "--web":
                    options.WebRoot = NextValue(args, ref i, arg);
                    break;

                case "--open":
                    options.OpenBrowser = true;
                    break;

                case "--no-open":
                    options.OpenBrowser = false;
                    break;

                case "--verbose":
                    options.Verbose = true;
                    break;

                case "-y":
                case "--yes":
                    options.AssumeYes = true;
                    break;

                default:
                    throw new ArgumentException($"unknown option '{arg}'");
            }
        }

        return options;
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length) throw new ArgumentException($"{option} needs a value");
        return args[++index];
    }
}
