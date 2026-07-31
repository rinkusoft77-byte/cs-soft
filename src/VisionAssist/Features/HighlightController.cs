using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VisionAssist;

/// <summary>
/// Recolours the round objectives - the bomb, defuse kits and hostages - so they
/// do not disappear into the floor texture. These are all things every player is
/// already allowed to see; only their colour changes.
/// </summary>
public sealed class HighlightController
{
    private readonly VisionAssistPlugin _plugin;

    public HighlightController(VisionAssistPlugin plugin) => _plugin = plugin;

    private VisionAssistConfig Config => _plugin.Config;

    /// <summary>Ground items - these are only ever world models, so always safe to tint.</summary>
    private static readonly string[] StaticEntities = { "planted_c4", "hostage_entity" };

    /// <summary>Pickups that a player can be holding; skipped while carried.</summary>
    private static readonly string[] WeaponEntities = { "weapon_c4", "item_defuser", "weapon_defuser" };

    public void Tick()
    {
        if (!Config.HighlightEnabled) return;

        var bomb = ColorParser.ParseOrDefault(Config.HighlightBombColor, Color.Red);
        var defuser = ColorParser.ParseOrDefault(Config.HighlightDefuserColor, Color.Cyan);
        var hostage = ColorParser.ParseOrDefault(Config.HighlightHostageColor, Color.Gold);

        PaintStatic("planted_c4", bomb);
        PaintStatic("hostage_entity", hostage);

        PaintDropped("weapon_c4", bomb);
        PaintDropped("item_defuser", defuser);
        PaintDropped("weapon_defuser", defuser);
    }

    private static void PaintStatic(string designerName, Color color)
    {
        foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CBaseModelEntity>(designerName))
        {
            if (entity.IsValid) Tint(entity, color);
        }
    }

    private static void PaintDropped(string designerName, Color color)
    {
        foreach (var weapon in Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>(designerName))
        {
            if (!weapon.IsValid) continue;

            // Only tint what is lying on the ground. Recolouring a carried bomb
            // would repaint the world model on the carrier's back and tell the
            // enemy team who is holding it.
            if (weapon.OwnerEntity.Value is not null) continue;

            Tint(weapon, color);
        }
    }

    private static void Tint(CBaseModelEntity entity, Color color)
    {
        entity.RenderMode = RenderMode_t.kRenderTransColor;
        entity.Render = Color.FromArgb(255, color.R, color.G, color.B);

        Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender");
    }

    public void ResetAll()
    {
        foreach (var designerName in StaticEntities.Concat(WeaponEntities))
        {
            foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CBaseModelEntity>(designerName))
            {
                if (!entity.IsValid) continue;

                entity.RenderMode = RenderMode_t.kRenderNormal;
                entity.Render = Color.FromArgb(255, 255, 255, 255);

                Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender");
            }
        }
    }
}
