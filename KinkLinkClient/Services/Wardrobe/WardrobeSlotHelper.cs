using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;

namespace KinkLinkClient.Services;

public static class WardrobeSlotHelper
{
    public static readonly string[] AllSlotNames =
        ["Head", "Body", "Hands", "Legs", "Feet", "Ears", "Neck", "Wrists", "RFinger", "LFinger"];

    public static GlamourerEquipmentSlot GetSlotFromName(string slotName)
    {
        return slotName switch
        {
            "Head" => GlamourerEquipmentSlot.Head,
            "Body" => GlamourerEquipmentSlot.Body,
            "Hands" => GlamourerEquipmentSlot.Hands,
            "Legs" => GlamourerEquipmentSlot.Legs,
            "Feet" => GlamourerEquipmentSlot.Feet,
            "Ears" => GlamourerEquipmentSlot.Ears,
            "Neck" => GlamourerEquipmentSlot.Neck,
            "Wrists" => GlamourerEquipmentSlot.Wrists,
            "RFinger" => GlamourerEquipmentSlot.RFinger,
            "LFinger" => GlamourerEquipmentSlot.LFinger,
            _ => GlamourerEquipmentSlot.None,
        };
    }

    public static string GetNameFromSlot(GlamourerEquipmentSlot slot)
    {
        return slot switch
        {
            GlamourerEquipmentSlot.Head => "Head",
            GlamourerEquipmentSlot.Body => "Body",
            GlamourerEquipmentSlot.Hands => "Hands",
            GlamourerEquipmentSlot.Legs => "Legs",
            GlamourerEquipmentSlot.Feet => "Feet",
            GlamourerEquipmentSlot.Ears => "Ears",
            GlamourerEquipmentSlot.Neck => "Neck",
            GlamourerEquipmentSlot.Wrists => "Wrists",
            GlamourerEquipmentSlot.RFinger => "RFinger",
            GlamourerEquipmentSlot.LFinger => "LFinger",
            _ => "None",
        };
    }

    public static void SetEquipmentSlot(
        GlamourerEquipment equipment,
        GlamourerEquipmentSlot slot,
        GlamourerItem item
    )
    {
        switch (slot)
        {
            case GlamourerEquipmentSlot.Head:
                equipment.Head = item;
                break;
            case GlamourerEquipmentSlot.Body:
                equipment.Body = item;
                break;
            case GlamourerEquipmentSlot.Hands:
                equipment.Hands = item;
                break;
            case GlamourerEquipmentSlot.Legs:
                equipment.Legs = item;
                break;
            case GlamourerEquipmentSlot.Feet:
                equipment.Feet = item;
                break;
            case GlamourerEquipmentSlot.Ears:
                equipment.Ears = item;
                break;
            case GlamourerEquipmentSlot.Neck:
                equipment.Neck = item;
                break;
            case GlamourerEquipmentSlot.Wrists:
                equipment.Wrists = item;
                break;
            case GlamourerEquipmentSlot.RFinger:
                equipment.RFinger = item;
                break;
            case GlamourerEquipmentSlot.LFinger:
                equipment.LFinger = item;
                break;
        }
    }

    public static bool EquippedItemsChanged(
        GlamourerEquipment activeset,
        GlamourerEquipment newState
    )
    {
        if (activeset.Head.Apply && activeset.Head.IsEqualTo(newState.Head) is false)
            return true;
        if (activeset.Body.Apply && activeset.Body.IsEqualTo(newState.Body) is false)
            return true;
        if (activeset.Hands.Apply && activeset.Hands.IsEqualTo(newState.Hands) is false)
            return true;
        if (activeset.Legs.Apply && activeset.Legs.IsEqualTo(newState.Legs) is false)
            return true;
        if (activeset.Feet.Apply && activeset.Feet.IsEqualTo(newState.Feet) is false)
            return true;
        if (activeset.Ears.Apply && activeset.Ears.IsEqualTo(newState.Ears) is false)
            return true;
        if (activeset.Neck.Apply && activeset.Neck.IsEqualTo(newState.Neck) is false)
            return true;
        if (activeset.Wrists.Apply && activeset.Wrists.IsEqualTo(newState.Wrists) is false)
            return true;
        if (activeset.RFinger.Apply && activeset.RFinger.IsEqualTo(newState.RFinger) is false)
            return true;
        if (activeset.LFinger.Apply && activeset.LFinger.IsEqualTo(newState.LFinger) is false)
            return true;
        return false;
    }
}
