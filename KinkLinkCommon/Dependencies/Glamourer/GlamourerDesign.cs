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
                if (other.Equipment.Head.Apply)
                    copy.Equipment.Head = other.Equipment.Head.Clone();
                break;
            case WardrobeLayer.Hands:
                if (other.Equipment.Hands.Apply)
                    copy.Equipment.Hands = other.Equipment.Hands.Clone();
                break;
            case WardrobeLayer.Legs:
                if (other.Equipment.Legs.Apply)
                    copy.Equipment.Legs = other.Equipment.Legs.Clone();
                break;
            case WardrobeLayer.Feet:
                if (other.Equipment.Feet.Apply)
                    copy.Equipment.Feet = other.Equipment.Feet.Clone();
                break;
            case WardrobeLayer.Ears:
                if (other.Equipment.Ears.Apply)
                    copy.Equipment.Ears = other.Equipment.Ears.Clone();
                break;
            case WardrobeLayer.Neck:
                if (other.Equipment.Neck.Apply)
                    copy.Equipment.Neck = other.Equipment.Neck.Clone();
                break;
            case WardrobeLayer.Wrists:
                if (other.Equipment.Wrists.Apply)
                    copy.Equipment.Wrists = other.Equipment.Wrists.Clone();
                break;
            case WardrobeLayer.RFinger:
                if (other.Equipment.RFinger.Apply)
                    copy.Equipment.RFinger = other.Equipment.RFinger.Clone();
                break;
            case WardrobeLayer.LFinger:
                if (other.Equipment.LFinger.Apply)
                    copy.Equipment.LFinger = other.Equipment.LFinger.Clone();
                break;
            case WardrobeLayer.Mods:
                // Mods layer not handled here yet
                break;
            default:
                // Other layers apply all types of stuff based on the underlying application flags
                if (other.Equipment.MainHand.Apply)
                    copy.Equipment.MainHand = other.Equipment.MainHand.Clone();
                if (other.Equipment.OffHand.Apply)
                    copy.Equipment.OffHand = other.Equipment.OffHand.Clone();
                if (other.Equipment.Head.Apply)
                    copy.Equipment.Head = other.Equipment.Head.Clone();
                if (other.Equipment.Body.Apply)
                    copy.Equipment.Body = other.Equipment.Body.Clone();
                if (other.Equipment.Hands.Apply)
                    copy.Equipment.Hands = other.Equipment.Hands.Clone();
                if (other.Equipment.Legs.Apply)
                    copy.Equipment.Legs = other.Equipment.Legs.Clone();
                if (other.Equipment.Feet.Apply)
                    copy.Equipment.Feet = other.Equipment.Feet.Clone();
                if (other.Equipment.Ears.Apply)
                    copy.Equipment.Ears = other.Equipment.Ears.Clone();
                if (other.Equipment.Neck.Apply)
                    copy.Equipment.Neck = other.Equipment.Neck.Clone();
                if (other.Equipment.Wrists.Apply)
                    copy.Equipment.Wrists = other.Equipment.Wrists.Clone();
                if (other.Equipment.RFinger.Apply)
                    copy.Equipment.RFinger = other.Equipment.RFinger.Clone();
                if (other.Equipment.LFinger.Apply)
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
