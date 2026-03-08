using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerValue
{
    [Key(0)]
    public bool Apply;

    [Key(1)]
    public uint Value;

    public GlamourerValue Clone() => (GlamourerValue)MemberwiseClone();

    public bool IsEqualTo(GlamourerValue other)
    {
        if (Apply != other.Apply)
            return false;
        if (Value != other.Value)
            return false;
        return true;
    }
}
