namespace VisionAssist.Companion;

/// <summary>
/// Turns a GSI weapon id into something readable at a glance. Anything not in
/// the table falls back to the id with the prefix stripped, so a weapon added
/// by a game update still shows a sensible name instead of nothing.
/// </summary>
public static class WeaponNames
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["weapon_knife"] = "Knife",
        ["weapon_knife_t"] = "Knife",
        ["weapon_taser"] = "Zeus",
        ["weapon_c4"] = "C4",
        ["weapon_healthshot"] = "Healthshot",

        ["weapon_hegrenade"] = "HE",
        ["weapon_flashbang"] = "Flash",
        ["weapon_smokegrenade"] = "Smoke",
        ["weapon_molotov"] = "Molotov",
        ["weapon_incgrenade"] = "Incendiary",
        ["weapon_decoy"] = "Decoy",

        ["weapon_glock"] = "Glock-18",
        ["weapon_hkp2000"] = "P2000",
        ["weapon_usp_silencer"] = "USP-S",
        ["weapon_p250"] = "P250",
        ["weapon_fiveseven"] = "Five-SeveN",
        ["weapon_tec9"] = "Tec-9",
        ["weapon_cz75a"] = "CZ75-Auto",
        ["weapon_deagle"] = "Desert Eagle",
        ["weapon_revolver"] = "R8 Revolver",
        ["weapon_elite"] = "Dual Berettas",

        ["weapon_mac10"] = "MAC-10",
        ["weapon_mp9"] = "MP9",
        ["weapon_mp7"] = "MP7",
        ["weapon_mp5sd"] = "MP5-SD",
        ["weapon_ump45"] = "UMP-45",
        ["weapon_p90"] = "P90",
        ["weapon_bizon"] = "PP-Bizon",

        ["weapon_famas"] = "FAMAS",
        ["weapon_galilar"] = "Galil AR",
        ["weapon_m4a1"] = "M4A4",
        ["weapon_m4a1_silencer"] = "M4A1-S",
        ["weapon_ak47"] = "AK-47",
        ["weapon_aug"] = "AUG",
        ["weapon_sg556"] = "SG 553",
        ["weapon_ssg08"] = "SSG 08",
        ["weapon_awp"] = "AWP",
        ["weapon_scar20"] = "SCAR-20",
        ["weapon_g3sg1"] = "G3SG1",

        ["weapon_nova"] = "Nova",
        ["weapon_xm1014"] = "XM1014",
        ["weapon_sawedoff"] = "Sawed-Off",
        ["weapon_mag7"] = "MAG-7",
        ["weapon_m249"] = "M249",
        ["weapon_negev"] = "Negev",
    };

    public static string Pretty(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return string.Empty;
        if (Known.TryGetValue(id, out string? name)) return name;

        // Every knife skin is its own id (weapon_bayonet, weapon_knife_karambit
        // and so on), and there is no point listing them all.
        if (id.Contains("knife", StringComparison.OrdinalIgnoreCase)
            || id.Contains("bayonet", StringComparison.OrdinalIgnoreCase))
        {
            return "Knife";
        }

        string trimmed = id.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
            ? id["weapon_".Length..]
            : id;

        return trimmed.Replace('_', ' ').ToUpperInvariant();
    }
}
