using System;
using System.Collections.Generic;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer;

[MessagePackObject]
public class GlamourerDesign
{
    [Key(0)]
    public int FileVersion;

    [Key(1)]
    public Guid Identifier;

    [Key(2)]
    public DateTimeOffset CreationDate;

    [Key(3)]
    public DateTimeOffset LastEdit;

    [Key(4)]
    public string Name = string.Empty;

    [Key(5)]
    public string Description = string.Empty;

    [Key(6)]
    public bool ForcedRedraw;

    [Key(7)]
    public bool ResetAdvancedDyes;

    [Key(8)]
    public bool ResetTemporarySettings;

    [Key(9)]
    public string Color = string.Empty;

    [Key(10)]
    public bool QuickDesign = true;

    [Key(11)]
    public string[] Tags = [];

    [Key(12)]
    public bool WriteProtected;

    [Key(13)]
    public GlamourerEquipment Equipment = new();

    [Key(14)]
    public GlamourerBonus Bonus = new();

    [Key(15)]
    public GlamourerCustomize Customize = new();

    [Key(16)]
    public GlamourerParameter Parameters = new();

    [Key(17)]
    public Dictionary<string, GlamourerMaterial> Materials = [];

    [Key(18)]
    public List<GlamourerMod> Mods = [];

    public GlamourerDesign Merge(GlamourerDesign other, WardrobeLayer layer)
    {
        // Insert "It ain't pretty, but it works".gif
        if (other == null)
            return this;

        // Work on a copy
        var copy = Clone();

        switch (layer)
        {
            case WardrobeLayer.Head:
                copy.Equipment.Head = other.Equipment.Head.Clone();
                copy.Equipment.Head.Apply = true;
                break;
            case WardrobeLayer.Hands:
                copy.Equipment.Hands = other.Equipment.Hands.Clone();
                copy.Equipment.Hands.Apply = true;
                break;
            case WardrobeLayer.Legs:
                copy.Equipment.Legs = other.Equipment.Legs.Clone();
                copy.Equipment.Legs.Apply = true;
                break;
            case WardrobeLayer.Feet:
                copy.Equipment.Feet = other.Equipment.Feet.Clone();
                copy.Equipment.Feet.Apply = true;
                break;
            case WardrobeLayer.Ears:
                copy.Equipment.Ears = other.Equipment.Ears.Clone();
                copy.Equipment.Ears.Apply = true;
                break;
            case WardrobeLayer.Neck:
                copy.Equipment.Neck = other.Equipment.Neck.Clone();
                copy.Equipment.Neck.Apply = true;
                break;
            case WardrobeLayer.Wrists:
                copy.Equipment.Wrists = other.Equipment.Wrists.Clone();
                copy.Equipment.Wrists.Apply = true;
                break;
            case WardrobeLayer.RFinger:
                copy.Equipment.RFinger = other.Equipment.RFinger.Clone();
                copy.Equipment.RFinger.Apply = true;
                break;
            case WardrobeLayer.LFinger:
                copy.Equipment.LFinger = other.Equipment.LFinger.Clone();
                copy.Equipment.LFinger.Apply = true;
                break;
            case WardrobeLayer.Mods:
                // TODO: Handle mod layers
                break;
            default:
                // Other layers apply all types of stuff based on the underlying application flags
                if (other.Equipment.MainHand.Apply && other.Equipment.MainHand.ItemId != 0)
                    copy.Equipment.MainHand = other.Equipment.MainHand.Clone();
                if (other.Equipment.OffHand.Apply && other.Equipment.OffHand.ItemId != 0)
                    copy.Equipment.OffHand = other.Equipment.OffHand.Clone();
                if (other.Equipment.Head.Apply && other.Equipment.Head.ItemId != 0)
                    copy.Equipment.Head = other.Equipment.Head.Clone();
                if (other.Equipment.Body.Apply && other.Equipment.Body.ItemId != 0)
                    copy.Equipment.Body = other.Equipment.Body.Clone();
                if (other.Equipment.Hands.Apply && other.Equipment.Hands.ItemId != 0)
                    copy.Equipment.Hands = other.Equipment.Hands.Clone();
                if (other.Equipment.Legs.Apply && other.Equipment.Legs.ItemId != 0)
                    copy.Equipment.Legs = other.Equipment.Legs.Clone();
                if (other.Equipment.Feet.Apply && other.Equipment.Feet.ItemId != 0)
                    copy.Equipment.Feet = other.Equipment.Feet.Clone();
                if (other.Equipment.Ears.Apply && other.Equipment.Ears.ItemId != 0)
                    copy.Equipment.Ears = other.Equipment.Ears.Clone();
                if (other.Equipment.Neck.Apply && other.Equipment.Neck.ItemId != 0)
                    copy.Equipment.Neck = other.Equipment.Neck.Clone();
                if (other.Equipment.Wrists.Apply && other.Equipment.Wrists.ItemId != 0)
                    copy.Equipment.Wrists = other.Equipment.Wrists.Clone();
                if (other.Equipment.RFinger.Apply && other.Equipment.RFinger.ItemId != 0)
                    copy.Equipment.RFinger = other.Equipment.RFinger.Clone();
                if (other.Equipment.LFinger.Apply && other.Equipment.LFinger.ItemId != 0)
                    copy.Equipment.LFinger = other.Equipment.LFinger.Clone();

                // Shows / toggles use Apply flag as well
                if (other.Equipment.Hat.Apply)
                    copy.Equipment.Hat = other.Equipment.Hat.Clone();
                if (other.Equipment.VieraEars.Apply)
                    copy.Equipment.VieraEars = other.Equipment.VieraEars.Clone();
                if (other.Equipment.Weapon.Apply)
                    copy.Equipment.Weapon = other.Equipment.Weapon.Clone();
                if (other.Equipment.Visor.Apply)
                    copy.Equipment.Visor = other.Equipment.Visor.Clone();
                break;
        }

        return copy;
    }

    public GlamourerDesign Clone()
    {
        var tags = new string[Tags.Length];
        for (var i = 0; i < Tags.Length; i++)
            tags[i] = Tags[i];

        var materials = new Dictionary<string, GlamourerMaterial>();
        foreach (var material in Materials)
            materials[material.Key] = material.Value.Clone();

        var copy = (GlamourerDesign)MemberwiseClone();

        copy.Tags = tags;
        copy.Materials = materials;

        copy.Equipment = Equipment.Clone();
        copy.Bonus = Bonus.Clone();
        copy.Customize = Customize.Clone();
        copy.Parameters = Parameters.Clone();

        return copy;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
