using System.Drawing;

namespace VisionAssist.Overlay;

/// <summary>
/// The same seven colour presets the server plugin and the browser overlay use,
/// so a player who has settled on one does not have to find it again here.
/// </summary>
internal sealed record Palette(string Key, string Description, Color TeamT, Color TeamCt)
{
    public static readonly IReadOnlyList<Palette> All = new[]
    {
        new Palette("highcontrast", "sariq / moviy",
            Color.FromArgb(0xFF, 0xEB, 0x3C), Color.FromArgb(0x00, 0xD9, 0xFF)),

        new Palette("neon", "pushti / yashil",
            Color.FromArgb(0xFF, 0x2E, 0xD1), Color.FromArgb(0x39, 0xFF, 0x14)),

        new Palette("deuteranopia", "to'q sariq / ko'k",
            Color.FromArgb(0xFF, 0x8C, 0x00), Color.FromArgb(0x00, 0x72, 0xB2)),

        new Palette("protanopia", "qahrabo / osmoniy",
            Color.FromArgb(0xE6, 0x9F, 0x00), Color.FromArgb(0x56, 0xB4, 0xE9)),

        new Palette("tritanopia", "qizil / firuza",
            Color.FromArgb(0xFF, 0x4B, 0x4B), Color.FromArgb(0x00, 0xC2, 0xA0)),

        new Palette("soft", "pastel",
            Color.FromArgb(0xFF, 0xB3, 0xC7), Color.FromArgb(0xA8, 0xD8, 0xFF)),

        new Palette("mono", "oq / kulrang",
            Color.FromArgb(0xFF, 0xFF, 0xFF), Color.FromArgb(0x9E, 0x9E, 0x9E)),
    };

    // Fixed regardless of preset: these mean the same thing every time, and
    // recolouring them per theme would make them harder to learn.
    public static readonly Color Background = Color.FromArgb(0x12, 0x16, 0x1C);
    public static readonly Color Edge = Color.FromArgb(0x26, 0x2E, 0x39);
    public static readonly Color Foreground = Color.FromArgb(0xF2, 0xF5, 0xF8);
    public static readonly Color Muted = Color.FromArgb(0x8A, 0x97, 0xA6);
    public static readonly Color Danger = Color.FromArgb(0xFF, 0x3B, 0x30);
    public static readonly Color Warn = Color.FromArgb(0xFF, 0xB0, 0x20);
    public static readonly Color Ok = Color.FromArgb(0x2F, 0xD1, 0x6B);

    public static Palette Find(string? key)
        => All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? All[0];

    /// <summary>The accent for a team: "T", "CT" or anything else.</summary>
    public Color Accent(string? team)
        => string.Equals(team, "T", StringComparison.OrdinalIgnoreCase) ? TeamT : TeamCt;
}
