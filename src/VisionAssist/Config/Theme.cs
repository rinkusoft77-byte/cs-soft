namespace VisionAssist;

/// <summary>
/// A ready-made pair of team colours. The point of the presets is that picking
/// good colours is the hard part: they have to stay apart from each other AND
/// from the sand/concrete backdrops the maps are built from.
/// </summary>
public sealed record Theme(string Key, string Description, string ColorT, string ColorCT)
{
    public static readonly IReadOnlyList<Theme> All = new[]
    {
        new Theme("highcontrast", "Yellow / cyan - brightest against sand and concrete",
            "#FFEB3C", "#00D9FF"),

        new Theme("neon", "Magenta / green - maximum separation from every map palette",
            "#FF2ED1", "#39FF14"),

        new Theme("deuteranopia", "Orange / blue - safe for red-green colour blindness",
            "#FF8C00", "#0072B2"),

        new Theme("protanopia", "Amber / sky - safe for red-weak vision",
            "#E69F00", "#56B4E9"),

        new Theme("tritanopia", "Red / teal - safe for blue-yellow colour blindness",
            "#FF4B4B", "#00C2A0"),

        new Theme("soft", "Pastel pink / pale blue - lower glare for light sensitivity",
            "#FFB3C7", "#A8D8FF"),

        new Theme("mono", "White / black - pure luminance contrast, no hue needed",
            "#FFFFFF", "#101010"),
    };

    public static Theme? Find(string? key)
        => All.FirstOrDefault(t => string.Equals(t.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string KeyList() => string.Join(", ", All.Select(t => t.Key));
}
