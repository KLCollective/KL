using System;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public static class WardrobeSlotHelper
{
    public static KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot GetSlotFromName(
        string name
    )
    {
        return name switch
        {
            "Head" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Head,
            "Body" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Body,
            "Hands" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Hands,
            "Legs" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Legs,
            "Feet" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Feet,
            "Ears" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Ears,
            "Neck" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Neck,
            "Wrists" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Wrists,
            "RFinger" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.RFinger,
            "LFinger" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.LFinger,
            "BaseSet" => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.None,
            _ => KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.None,
        };
    }

    public static string GetNameFromSlot(WardrobeLayer layer)
    {
        return layer switch
        {
            WardrobeLayer.Head => "Head",
            WardrobeLayer.Chest => "Body",
            WardrobeLayer.Hands => "Hands",
            WardrobeLayer.Legs => "Legs",
            WardrobeLayer.Feet => "Feet",
            WardrobeLayer.Ears => "Ears",
            WardrobeLayer.Neck => "Neck",
            WardrobeLayer.Wrists => "Wrists",
            WardrobeLayer.RFinger => "RFinger",
            WardrobeLayer.LFinger => "LFinger",
            WardrobeLayer.Outfit => "Outfit",
            WardrobeLayer.Mods => "Mods",
            _ => layer.ToString(),
        };
    }

    public static WardrobeLayer GetLayerFromName(string name)
    {
        return name switch
        {
            "Head" => WardrobeLayer.Head,
            "Body" => WardrobeLayer.Chest,
            "Hands" => WardrobeLayer.Hands,
            "Legs" => WardrobeLayer.Legs,
            "Feet" => WardrobeLayer.Feet,
            "Ears" => WardrobeLayer.Ears,
            "Neck" => WardrobeLayer.Neck,
            "Wrists" => WardrobeLayer.Wrists,
            "RFinger" => WardrobeLayer.RFinger,
            "LFinger" => WardrobeLayer.LFinger,
            "BaseSet" => WardrobeLayer.Outfit,
            "Outfit" => WardrobeLayer.Outfit,
            "Mods" => WardrobeLayer.Mods,
            _ => WardrobeLayer.Outfit,
        };
    }

    /// <summary>
    ///     Returns true if any equipment slot changed between the two states.
    /// </summary>
    public static bool EquippedItemsChanged(GlamourerEquipment before, GlamourerEquipment after)
    {
        if (!before.MainHand.IsEqualTo(after.MainHand)) return true;
        if (!before.OffHand.IsEqualTo(after.OffHand)) return true;
        if (!before.Head.IsEqualTo(after.Head)) return true;
        if (!before.Body.IsEqualTo(after.Body)) return true;
        if (!before.Hands.IsEqualTo(after.Hands)) return true;
        if (!before.Legs.IsEqualTo(after.Legs)) return true;
        if (!before.Feet.IsEqualTo(after.Feet)) return true;
        if (!before.Ears.IsEqualTo(after.Ears)) return true;
        if (!before.Neck.IsEqualTo(after.Neck)) return true;
        if (!before.Wrists.IsEqualTo(after.Wrists)) return true;
        if (!before.RFinger.IsEqualTo(after.RFinger)) return true;
        if (!before.LFinger.IsEqualTo(after.LFinger)) return true;
        if (!before.Hat.IsEqualTo(after.Hat)) return true;
        if (!before.VieraEars.IsEqualTo(after.VieraEars)) return true;
        if (!before.Weapon.IsEqualTo(after.Weapon)) return true;
        if (!before.Visor.IsEqualTo(after.Visor)) return true;
        return false;
    }
}

