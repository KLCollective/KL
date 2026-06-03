using System;
using System.Collections.Generic;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public record WardrobeItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WardrobeLayer Layer = WardrobeLayer.Outfit;

    private GlamourerDesign _design = new GlamourerDesign();
    public GlamourerDesign Design
    {
        get => _design;
        set
        {
            _design = value ?? new GlamourerDesign();
            // Id is server-generated or explicitly assigned — never derive from design Identifier.
            // Only generate a new GUID as a local temporary key if Id hasn't been set yet.
            if (Id == Guid.Empty)
            {
                Id = Guid.NewGuid();
            }

            // Only populate name/description when not already set to avoid overwriting user-provided values
            if (string.IsNullOrEmpty(Name))
                Name = _design.Name;
            if (string.IsNullOrEmpty(Description))
                Description = _design.Description;
        }
    }

    public RelationshipPriority Priority { get; set; } = RelationshipPriority.Casual;

    // Compatibility: expose Slot and Item to match older UI expectations
    public GlamourerEquipmentSlot Slot
    {
        get =>
            Layer switch
            {
                WardrobeLayer.Outfit => GlamourerEquipmentSlot.None,
                WardrobeLayer.Head => GlamourerEquipmentSlot.Head,
                WardrobeLayer.Chest => GlamourerEquipmentSlot.Body,
                WardrobeLayer.Hands => GlamourerEquipmentSlot.Hands,
                WardrobeLayer.Legs => GlamourerEquipmentSlot.Legs,
                WardrobeLayer.Feet => GlamourerEquipmentSlot.Feet,
                WardrobeLayer.Ears => GlamourerEquipmentSlot.Ears,
                WardrobeLayer.Neck => GlamourerEquipmentSlot.Neck,
                WardrobeLayer.Wrists => GlamourerEquipmentSlot.Wrists,
                WardrobeLayer.RFinger => GlamourerEquipmentSlot.RFinger,
                WardrobeLayer.LFinger => GlamourerEquipmentSlot.LFinger,
                WardrobeLayer.Mods => GlamourerEquipmentSlot.None,
                _ => GlamourerEquipmentSlot.None,
            };
        set
        {
            Layer = value switch
            {
                GlamourerEquipmentSlot.Head => WardrobeLayer.Head,
                GlamourerEquipmentSlot.Body => WardrobeLayer.Chest,
                GlamourerEquipmentSlot.Hands => WardrobeLayer.Hands,
                GlamourerEquipmentSlot.Legs => WardrobeLayer.Legs,
                GlamourerEquipmentSlot.Feet => WardrobeLayer.Feet,
                GlamourerEquipmentSlot.Ears => WardrobeLayer.Ears,
                GlamourerEquipmentSlot.Neck => WardrobeLayer.Neck,
                GlamourerEquipmentSlot.Wrists => WardrobeLayer.Wrists,
                GlamourerEquipmentSlot.RFinger => WardrobeLayer.RFinger,
                GlamourerEquipmentSlot.LFinger => WardrobeLayer.LFinger,
                _ => WardrobeLayer.Outfit,
            };
        }
    }

    public GlamourerItem? Item
    {
        get
        {
            return Layer switch
            {
                WardrobeLayer.Head => Design.Equipment.Head,
                WardrobeLayer.Chest => Design.Equipment.Body,
                WardrobeLayer.Hands => Design.Equipment.Hands,
                WardrobeLayer.Legs => Design.Equipment.Legs,
                WardrobeLayer.Feet => Design.Equipment.Feet,
                WardrobeLayer.Ears => Design.Equipment.Ears,
                WardrobeLayer.Neck => Design.Equipment.Neck,
                WardrobeLayer.Wrists => Design.Equipment.Wrists,
                WardrobeLayer.RFinger => Design.Equipment.RFinger,
                WardrobeLayer.LFinger => Design.Equipment.LFinger,
                _ => null,
            };
        }
        set
        {
            if (value == null)
                return;

            switch (Layer)
            {
                case WardrobeLayer.Head:
                    Design.Equipment.Head = value.Clone();
                    break;
                case WardrobeLayer.Chest:
                    Design.Equipment.Body = value.Clone();
                    break;
                case WardrobeLayer.Hands:
                    Design.Equipment.Hands = value.Clone();
                    break;
                case WardrobeLayer.Legs:
                    Design.Equipment.Legs = value.Clone();
                    break;
                case WardrobeLayer.Feet:
                    Design.Equipment.Feet = value.Clone();
                    break;
                case WardrobeLayer.Ears:
                    Design.Equipment.Ears = value.Clone();
                    break;
                case WardrobeLayer.Neck:
                    Design.Equipment.Neck = value.Clone();
                    break;
                case WardrobeLayer.Wrists:
                    Design.Equipment.Wrists = value.Clone();
                    break;
                case WardrobeLayer.RFinger:
                    Design.Equipment.RFinger = value.Clone();
                    break;
                case WardrobeLayer.LFinger:
                    Design.Equipment.LFinger = value.Clone();
                    break;
                default:
                    break;
            }
        }
    }

    // Compatibility property for older code expecting Mods as property
    public List<GlamourerMod> Mods
    {
        get => Design.Mods;
        set => Design.Mods = value ?? new List<GlamourerMod>();
    }
}
