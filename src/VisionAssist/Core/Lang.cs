namespace VisionAssist;

/// <summary>
/// Message catalogue. Kept in code rather than in lang/*.json so the plugin is
/// a single file to deploy - there is no extra folder to forget to copy.
/// </summary>
public static class Lang
{
    public static readonly string[] Supported = { "uz", "ru", "en" };

    private const string Fallback = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["uz"] = new()
        {
            ["announce"] = "Bu serverda {lime}ko'rinuvchanlik yordami{default} yoqilgan. Sozlash uchun {lime}!vision{default} yozing.",
            ["no_access"] = "Bu buyruq uchun ruxsatingiz yo'q.",
            ["on"] = "yoqildi",
            ["off"] = "o'chirildi",
            ["bad_toggle"] = "'{0}' noto'g'ri. {lime}on{default} yoki {lime}off{default} deb yozing.",
            ["bad_color"] = "'{0}' noma'lum rang. #RRGGBB, r,g,b yoki nom: {1}",
            ["bad_team"] = "Jamoa {lime}t{default}, {lime}ct{default} yoki {lime}all{default} bo'lishi kerak.",
            ["bad_number"] = "'{0}' son emas.",
            ["saved"] = "Saqlandi.",
            ["reloaded"] = "Konfiguratsiya qayta o'qildi.",
            ["reload_failed"] = "Qayta o'qishda xato: {0}",

            ["model_color"] = "Model rangi",
            ["outline"] = "Kontur",
            ["radar"] = "To'liq radar",
            ["highlight"] = "Bomba/garov belgilash",
            ["hud"] = "Katta ko'rsatkich (HUD)",
            ["theme_set"] = "Rang to'plami: {lime}{0}{default} ({1})",
            ["theme_unknown"] = "'{0}' bunday to'plam yo'q. Mavjudlari: {1}",
            ["color_set"] = "{0} rangi ({1}): {lime}{2}{default}",
            ["value_set"] = "{0}: {lime}{1}{default}",
            ["toggled"] = "{0} {1}.",

            ["lang_set"] = "Til o'zbekchaga o'zgartirildi.",
            ["lang_unknown"] = "'{0}' tili yo'q. Mavjud: {1}",
            ["hud_size_set"] = "HUD o'lchami: {lime}{0}{default}",
            ["hud_size_bad"] = "O'lcham {lime}s{default}, {lime}m{default} yoki {lime}l{default} bo'lishi kerak.",
            ["cfg_sent"] = "Tavsiya etilgan sozlamalar konsolga yozildi ({lime}~{default} tugmasi).",
            ["cfg_applied"] = "Radar sozlamalari qo'llandi.",

            ["colors_list"] = "Ranglar: {lime}{0}{default}",
            ["colors_hint"] = "Yoki {lime}#FF2ED1{default} yoki {lime}255,46,209{default} ko'rinishida yozing.",
            ["themes_list"] = "Rang to'plamlari:",

            ["menu_title"] = "Ko'rinuvchanlik sozlamalari",
            ["menu_hud_toggle"] = "HUD: {0}",
            ["menu_hud_size"] = "HUD o'lchami: {0}",
            ["menu_language"] = "Til: o'zbekcha",
            ["menu_client_cfg"] = "Radarni kattalashtirish",
            ["menu_themes"] = "Rang to'plamlari ro'yxati",
            ["menu_status"] = "Hozirgi holat",
            ["menu_admin"] = "--- Admin ---",
            ["menu_admin_theme"] = "Rang to'plamini almashtirish",
            ["menu_admin_tint"] = "Model rangi: {0}",
            ["menu_admin_outline"] = "Kontur: {0}",
            ["menu_admin_radar"] = "Radar: {0}",
            ["menu_admin_highlight"] = "Belgilash: {0}",

            ["hud_hp"] = "JON",
            ["hud_armor"] = "ZIRH",
            ["hud_bomb"] = "BOMBA",
            ["hud_freeze"] = "TAYYORLAN",
            ["hud_alive"] = "TIRIK",

            ["status_title"] = "Holat:",
        },

        ["ru"] = new()
        {
            ["announce"] = "На этом сервере включена {lime}помощь видимости{default}. Настройки: {lime}!vision{default}.",
            ["no_access"] = "У вас нет доступа к этой команде.",
            ["on"] = "включено",
            ["off"] = "выключено",
            ["bad_toggle"] = "'{0}' неверно. Напишите {lime}on{default} или {lime}off{default}.",
            ["bad_color"] = "'{0}' - неизвестный цвет. #RRGGBB, r,g,b или имя: {1}",
            ["bad_team"] = "Команда должна быть {lime}t{default}, {lime}ct{default} или {lime}all{default}.",
            ["bad_number"] = "'{0}' не является числом.",
            ["saved"] = "Сохранено.",
            ["reloaded"] = "Конфигурация перечитана.",
            ["reload_failed"] = "Ошибка перезагрузки: {0}",

            ["model_color"] = "Цвет модели",
            ["outline"] = "Контур",
            ["radar"] = "Полный радар",
            ["highlight"] = "Подсветка бомбы/заложников",
            ["hud"] = "Крупный индикатор (HUD)",
            ["theme_set"] = "Цветовая схема: {lime}{0}{default} ({1})",
            ["theme_unknown"] = "Схемы '{0}' нет. Доступные: {1}",
            ["color_set"] = "Цвет {0} ({1}): {lime}{2}{default}",
            ["value_set"] = "{0}: {lime}{1}{default}",
            ["toggled"] = "{0} {1}.",

            ["lang_set"] = "Язык переключён на русский.",
            ["lang_unknown"] = "Языка '{0}' нет. Доступные: {1}",
            ["hud_size_set"] = "Размер HUD: {lime}{0}{default}",
            ["hud_size_bad"] = "Размер должен быть {lime}s{default}, {lime}m{default} или {lime}l{default}.",
            ["cfg_sent"] = "Рекомендуемые настройки выведены в консоль (клавиша {lime}~{default}).",
            ["cfg_applied"] = "Настройки радара применены.",

            ["colors_list"] = "Цвета: {lime}{0}{default}",
            ["colors_hint"] = "Или напишите {lime}#FF2ED1{default} либо {lime}255,46,209{default}.",
            ["themes_list"] = "Цветовые схемы:",

            ["menu_title"] = "Настройки видимости",
            ["menu_hud_toggle"] = "HUD: {0}",
            ["menu_hud_size"] = "Размер HUD: {0}",
            ["menu_language"] = "Язык: русский",
            ["menu_client_cfg"] = "Увеличить радар",
            ["menu_themes"] = "Список цветовых схем",
            ["menu_status"] = "Текущее состояние",
            ["menu_admin"] = "--- Админ ---",
            ["menu_admin_theme"] = "Сменить цветовую схему",
            ["menu_admin_tint"] = "Цвет модели: {0}",
            ["menu_admin_outline"] = "Контур: {0}",
            ["menu_admin_radar"] = "Радар: {0}",
            ["menu_admin_highlight"] = "Подсветка: {0}",

            ["hud_hp"] = "HP",
            ["hud_armor"] = "БРОНЯ",
            ["hud_bomb"] = "БОМБА",
            ["hud_freeze"] = "ПОДГОТОВКА",
            ["hud_alive"] = "ЖИВЫХ",

            ["status_title"] = "Состояние:",
        },

        ["en"] = new()
        {
            ["announce"] = "This server runs {lime}visibility assistance{default}. Type {lime}!vision{default} to configure.",
            ["no_access"] = "You do not have access to this command.",
            ["on"] = "on",
            ["off"] = "off",
            ["bad_toggle"] = "'{0}' is not valid. Use {lime}on{default} or {lime}off{default}.",
            ["bad_color"] = "'{0}' is not a known colour. Use #RRGGBB, r,g,b or a name: {1}",
            ["bad_team"] = "Team must be {lime}t{default}, {lime}ct{default} or {lime}all{default}.",
            ["bad_number"] = "'{0}' is not a number.",
            ["saved"] = "Saved.",
            ["reloaded"] = "Config reloaded.",
            ["reload_failed"] = "Reload failed: {0}",

            ["model_color"] = "Model colour",
            ["outline"] = "Outline",
            ["radar"] = "Full radar",
            ["highlight"] = "Bomb/hostage highlight",
            ["hud"] = "Large readout (HUD)",
            ["theme_set"] = "Colour theme: {lime}{0}{default} ({1})",
            ["theme_unknown"] = "No theme called '{0}'. Available: {1}",
            ["color_set"] = "{0} colour ({1}): {lime}{2}{default}",
            ["value_set"] = "{0}: {lime}{1}{default}",
            ["toggled"] = "{0} {1}.",

            ["lang_set"] = "Language switched to English.",
            ["lang_unknown"] = "No language '{0}'. Available: {1}",
            ["hud_size_set"] = "HUD size: {lime}{0}{default}",
            ["hud_size_bad"] = "Size must be {lime}s{default}, {lime}m{default} or {lime}l{default}.",
            ["cfg_sent"] = "Recommended settings printed to your console ({lime}~{default} key).",
            ["cfg_applied"] = "Radar settings applied.",

            ["colors_list"] = "Colours: {lime}{0}{default}",
            ["colors_hint"] = "Or write {lime}#FF2ED1{default} or {lime}255,46,209{default}.",
            ["themes_list"] = "Colour themes:",

            ["menu_title"] = "Visibility settings",
            ["menu_hud_toggle"] = "HUD: {0}",
            ["menu_hud_size"] = "HUD size: {0}",
            ["menu_language"] = "Language: English",
            ["menu_client_cfg"] = "Enlarge radar",
            ["menu_themes"] = "List colour themes",
            ["menu_status"] = "Current status",
            ["menu_admin"] = "--- Admin ---",
            ["menu_admin_theme"] = "Cycle colour theme",
            ["menu_admin_tint"] = "Model colour: {0}",
            ["menu_admin_outline"] = "Outline: {0}",
            ["menu_admin_radar"] = "Radar: {0}",
            ["menu_admin_highlight"] = "Highlight: {0}",

            ["hud_hp"] = "HP",
            ["hud_armor"] = "ARMOR",
            ["hud_bomb"] = "BOMB",
            ["hud_freeze"] = "GET READY",
            ["hud_alive"] = "ALIVE",

            ["status_title"] = "Status:",
        },
    };

    public static bool IsSupported(string? language)
        => language is not null && Supported.Contains(language.Trim().ToLowerInvariant());

    public static string Get(string? language, string key, params object[] args)
    {
        var lang = language?.Trim().ToLowerInvariant() ?? Fallback;

        if (!Strings.TryGetValue(lang, out var table) || !table.TryGetValue(key, out var value))
        {
            if (!Strings[Fallback].TryGetValue(key, out value)) return key;
        }

        return args.Length == 0 ? value : string.Format(value, args);
    }

    public static string SupportedList() => string.Join(", ", Supported);
}
