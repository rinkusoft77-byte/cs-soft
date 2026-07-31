using System.Drawing;
using System.Net.Sockets;
using System.Windows.Forms;

namespace VisionAssist.Overlay;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // One overlay at a time: two would fight over the GSI port and stack two
        // HUDs on top of each other.
        using var single = new Mutex(true, @"Local\VisionAssist.Overlay.Instance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show(Strings.Get("alreadyRunning"), "VisionAssist",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var settings = OverlaySettings.Load();
        Strings.Language = settings.Language;

        using var sound = new SoundMeter { Sensitivity = settings.SoundSensitivity };
        using var gsi = new GsiHost(settings.Port, settings.Token);

        try
        {
            gsi.Start();
        }
        catch (SocketException)
        {
            MessageBox.Show($"{Strings.Get("portBusy")} (port {settings.Port})", "VisionAssist",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var form = new OverlayForm(settings, gsi, sound);
        using var tray = BuildTray(form);

        form.TrayNotified = message =>
            tray.ShowBalloonTip(5000, "VisionAssist", message, ToolTipIcon.Warning);

        form.Show();

        if (!GsiCfgInstalled())
        {
            tray.ShowBalloonTip(8000, "VisionAssist",
                Strings.Get("gsiMissing"), ToolTipIcon.Warning);
        }

        // No main form is passed: the overlay hides itself whenever CS2 is not in
        // front, and Application.Run(form) would quit the program the first time
        // that happened. The tray icon is what keeps the loop alive.
        Application.Run();

        settings.Save();
    }

    /// <summary>
    /// The only visible entry point into the program - the HUD has no title bar
    /// and no taskbar button, so without this there would be no way to reach the
    /// menu if the Alt hook failed to install.
    /// </summary>
    private static NotifyIcon BuildTray(OverlayForm form)
    {
        var menu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem(Strings.Get("settings"));
        settingsItem.Click += (_, _) => form.ToggleSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem(Strings.Get("quit"));
        quitItem.Click += (_, _) => Application.Exit();
        menu.Items.Add(quitItem);

        var tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "VisionAssist Overlay",
            Visible = true,
            ContextMenuStrip = menu,
        };

        tray.DoubleClick += (_, _) => form.ToggleSettings();
        return tray;
    }

    /// <summary>
    /// Whether any CS2 install has the GSI cfg in place. Without it the game
    /// sends nothing and the HUD would sit on "waiting" with no explanation.
    /// </summary>
    private static bool GsiCfgInstalled()
    {
        try
        {
            foreach (string folder in Companion.SteamLocator.FindCfgFolders())
            {
                if (File.Exists(Path.Combine(folder, Companion.CfgInstaller.GsiFileName)))
                    return true;
            }
        }
        catch (Exception)
        {
            // Cannot tell - better to say nothing than to warn wrongly.
            return true;
        }

        return false;
    }
}
