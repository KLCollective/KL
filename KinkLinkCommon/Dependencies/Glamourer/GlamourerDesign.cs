using System;
using System.Collections.Generic;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
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
