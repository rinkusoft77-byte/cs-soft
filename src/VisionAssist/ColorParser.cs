using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;

namespace VisionAssist;

/// <summary>
/// Turns the strings used in config/chat commands into RGB colors.
/// Accepts "#RRGGBB", "RRGGBB", "r,g,b" and the names below.
/// </summary>
public static class ColorParser
{
    /// <summary>
    /// High-contrast presets. Picked so each one stays distinguishable against
    /// Dust2/Mirage/Inferno backdrops rather than for being pretty.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Color> NamedColors =
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["magenta"] = Color.FromArgb(255, 46, 209),
            ["pink"] = Color.FromArgb(255, 105, 180),
            ["red"] = Color.FromArgb(255, 40, 40),
            ["orange"] = Color.FromArgb(255, 140, 0),
            ["yellow"] = Color.FromArgb(255, 235, 60),
            ["lime"] = Color.FromArgb(140, 255, 40),
            ["green"] = Color.FromArgb(0, 220, 80),
            ["cyan"] = Color.FromArgb(0, 217, 255),
            ["blue"] = Color.FromArgb(60, 120, 255),
            ["purple"] = Color.FromArgb(170, 80, 255),
            ["white"] = Color.FromArgb(255, 255, 255),
            ["black"] = Color.FromArgb(10, 10, 10),
        };

    public static bool TryParse(string? input, [NotNullWhen(true)] out Color? color)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var value = input.Trim();

        if (NamedColors.TryGetValue(value, out var named))
        {
            color = named;
            return true;
        }

        if (value.Contains(','))
        {
            var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;

            var rgb = new int[3];
            for (var i = 0; i < 3; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out rgb[i])) return false;
                if (rgb[i] < 0 || rgb[i] > 255) return false;
            }

            color = Color.FromArgb(rgb[0], rgb[1], rgb[2]);
            return true;
        }

        var hex = value.StartsWith('#') ? value[1..] : value;
        if (hex.Length != 6) return false;
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed)) return false;

        color = Color.FromArgb((packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
        return true;
    }

    /// <summary>Falls back to <paramref name="fallback"/> instead of throwing on bad config input.</summary>
    public static Color ParseOrDefault(string? input, Color fallback)
        => TryParse(input, out var color) ? color.Value : fallback;

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static string NamesList() => string.Join(", ", NamedColors.Keys);
}
