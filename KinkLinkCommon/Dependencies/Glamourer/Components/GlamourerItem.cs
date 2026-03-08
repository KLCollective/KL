using System.Text.Json.Serialization;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerItem
{
    [Key(0)]
    [JsonInclude]
    public bool Apply;

    [Key(1)]
    [JsonInclude]
    public bool ApplyCrest;

    [Key(2)]
    [JsonInclude]
    public bool ApplyStain;

    [Key(3)]
    [JsonInclude]
    public bool Crest;

    [Key(4)]
    [JsonInclude]
    public uint ItemId;

    [Key(5)]
    [JsonInclude]
    public uint Stain;

    [Key(6)]
    [JsonInclude]
    public uint Stain2;

    public GlamourerItem Clone() => (GlamourerItem)MemberwiseClone();

    public bool IsEqualTo(GlamourerItem other)
    {
        if (Apply != other.Apply) return false;
        if (ApplyCrest != other.ApplyCrest) return false;
        if (ApplyStain != other.ApplyStain) return false;
        if (Crest != other.Crest) return false;
        if (ItemId != other.ItemId) return false;
        if (Stain != other.Stain) return false;
        if (Stain2 != other.Stain2) return false;
        return true;
    }
}
