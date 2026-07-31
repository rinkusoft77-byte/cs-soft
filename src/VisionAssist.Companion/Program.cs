using System.Diagnostics;
using System.Net.Sockets;

namespace VisionAssist.Companion;

/// <summary>
/// Entry point. Unlike src/VisionAssist - a class library the CS2 server loads -
/// this project is a console application, so F5 in Visual Studio runs it.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Any(a => a is "-h" or "--help" or "/?" or "/help"))
        {
            PrintUsage();
            return 0;
        }

        CompanionOptions options;
        try
        {
            options = CompanionOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"visionassist: {ex.Message}");
            Console.Error.WriteLine("Try --help.");
            return 2;
        }

        var config = CompanionConfig.Load(CompanionConfig.DefaultPath, out string? notice);
        if (notice is not null) Console.WriteLine($"visionassist: {notice}");
        if (options.Port is int port) config.Port = port;

        if (options.Locate) return Locate();
        if (options.Install) return Install(config, options);

        return await RunAsync(config, options).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------- run

    private static async Task<int> RunAsync(CompanionConfig config, CompanionOptions options)
    {
        string webRoot = options.WebRoot ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var content = new StaticContent(webRoot);
        if (!content.Exists)
        {
            Console.Error.WriteLine($"visionassist: overlay folder not found: {content.Root}");
            Console.Error.WriteLine("It is copied next to the executable on build. Rebuild, or pass --web <folder>.");
            return 1;
        }

        var store = new StateStore();
        var events = new EventStream();
        var routes = new OverlayRoutes(config, store, events, content, options.Verbose);

        if (options.Verbose)
        {
            routes.SnapshotApplied += snapshot => Console.WriteLine(
                $"  gsi: hp={snapshot.Health} armor={snapshot.Armor} " +
                $"phase={snapshot.RoundPhase ?? "-"} bomb={snapshot.BombState ?? "-"} " +
                $"weapon={snapshot.WeaponName}");
        }

        using var server = new HttpServer(config.Port, routes.HandleAsync);
        try
        {
            server.Start();
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"visionassist: cannot listen on port {config.Port}: {ex.Message}");
            Console.Error.WriteLine("Another copy may already be running. Use --port <n> for a different port.");
            return 1;
        }

        string url = $"http://127.0.0.1:{config.Port}/";

        Console.WriteLine();
        Console.WriteLine("  VisionAssist Companion");
        Console.WriteLine($"  overlay   {url}");
        Console.WriteLine($"  gsi       {url}gsi");
        Console.WriteLine();
        ReportCfgStatus();
        Console.WriteLine("  Ctrl+C to stop.");
        Console.WriteLine();

        bool open = options.OpenBrowser ?? config.OpenBrowserOnStart;
        if (open) OpenBrowser(url);

        using var stopping = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            stopping.Cancel();
        };

        await WatchForStaleStateAsync(store, events, routes, stopping.Token).ConfigureAwait(false);

        Console.WriteLine("visionassist: stopped.");
        return 0;
    }

    /// <summary>
    /// The game promises a heartbeat every 10 s. When one stops arriving the
    /// overlay is told, so the page can grey out instead of showing a health bar
    /// frozen at whatever it was when CS2 closed.
    /// </summary>
    private static async Task WatchForStaleStateAsync(StateStore store, EventStream events,
        OverlayRoutes routes, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);

                if (store.Current.Connected && !store.IsFresh)
                {
                    var snapshot = store.MarkDisconnected();
                    events.Broadcast("state", routes.Serialize(snapshot));
                    Console.WriteLine("visionassist: no data from the game - is CS2 still running?");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C.
        }
    }

    // --------------------------------------------------------------- locate

    private static int Locate()
    {
        var folders = SteamLocator.FindCfgFolders();
        if (folders.Count == 0)
        {
            Console.WriteLine("visionassist: could not find the CS2 config folder.");
            Console.WriteLine("Find it yourself in Steam: right-click CS2 > Manage > Browse local files,");
            Console.WriteLine(@"then go into game\csgo\cfg and pass it with --cs2-cfg ""<path>"".");
            return 1;
        }

        Console.WriteLine("visionassist: CS2 config folder(s):");
        foreach (string folder in folders)
        {
            bool installed = File.Exists(Path.Combine(folder, CfgInstaller.GsiFileName));
            Console.WriteLine($"  {folder}{(installed ? "   [GSI cfg installed]" : string.Empty)}");
        }

        return 0;
    }

    // -------------------------------------------------------------- install

    private static int Install(CompanionConfig config, CompanionOptions options)
    {
        string? target = options.Cs2CfgPath;
        if (target is null)
        {
            var folders = SteamLocator.FindCfgFolders();
            if (folders.Count == 0)
            {
                Console.Error.WriteLine("visionassist: could not find the CS2 config folder.");
                Console.Error.WriteLine(@"Pass it explicitly: --install --cs2-cfg ""<path to game\csgo\cfg>""");
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Or create {CfgInstaller.GsiFileName} there yourself, with this content:");
                Console.Error.WriteLine();
                Console.Error.WriteLine(new CfgInstaller(config).RenderGsiCfg());
                return 1;
            }

            target = folders[0];
            if (folders.Count > 1)
            {
                Console.WriteLine("visionassist: more than one CS2 install found; using the first.");
                Console.WriteLine("Use --cs2-cfg to pick another. Found:");
                foreach (string folder in folders) Console.WriteLine($"  {folder}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("About to write into:");
        Console.WriteLine($"  {target}");
        Console.WriteLine();
        Console.WriteLine($"  {CfgInstaller.GsiFileName}           (read by CS2 on startup)");
        Console.WriteLine($"  {CfgInstaller.AccessibilityFileName}  (only runs when you exec it)");
        Console.WriteLine($"  {CfgInstaller.PerformanceFileName}    (only runs when you exec it)");
        Console.WriteLine();

        if (!options.AssumeYes && !Confirm())
        {
            Console.WriteLine("Nothing was written.");
            return 1;
        }


        try
        {
            var results = new CfgInstaller(config).Install(target);
            foreach (var result in results)
                Console.WriteLine($"  {(result.Overwrote ? "replaced" : "wrote")}  {result.Path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                      or DirectoryNotFoundException or FileNotFoundException)
        {
            Console.Error.WriteLine($"visionassist: install failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Next:");
        Console.WriteLine("  1. Restart CS2 - the GSI file is only read at startup.");
        Console.WriteLine("  2. Start this app and leave it running.");
        Console.WriteLine("  3. In the game console:  exec visionassist_accessibility");
        Console.WriteLine("                           exec visionassist_performance");
        return 0;
    }

    private static bool Confirm()
    {
        Console.Write("Write these two files? [y/N] ");
        string? answer = Console.ReadLine();
        return answer is not null
               && (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
                   || answer.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    // ----------------------------------------------------------------- misc

    private static void ReportCfgStatus()
    {
        var folders = SteamLocator.FindCfgFolders();
        bool installed = folders.Any(f => File.Exists(Path.Combine(f, CfgInstaller.GsiFileName)));

        if (installed) return;

        Console.WriteLine("  The game is not sending anything yet: the GSI cfg is not installed.");
        Console.WriteLine("  Run once with --install, then restart CS2.");
        Console.WriteLine();
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"visionassist: could not open a browser ({ex.Message}). Open {url} yourself.");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            VisionAssist Companion - local accessibility overlay for CS2.

            Reads your own game state through Valve's Game State Integration
            interface and draws it large and high-contrast, and turns the game
            audio you are already hearing into a visual direction indicator.
            Works on every server you join, because nothing about it touches the
            server or the game process.

            usage:
              VisionAssist.Companion [options]

            options:
              --install            write the .cfg files into the CS2 config folder
              --locate             print where the CS2 config folder was found
              --cs2-cfg <path>     use this config folder instead of searching
              -y, --yes            do not ask before writing during --install
              --port <n>           listen on this port instead of companion.json's
              --web <folder>       serve the overlay page from here
              --open / --no-open   launch a browser at startup, or do not
              --verbose            log every state update
              -h, --help           this text

            first run:
              VisionAssist.Companion --install
              restart CS2, then start this app and leave it running
            """);
    }
}
