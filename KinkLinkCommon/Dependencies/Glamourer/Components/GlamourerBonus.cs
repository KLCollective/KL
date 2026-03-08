using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerBonus
{
    [Key(0)]
    public bool Apply;

    [Key(1)]
    public ulong BonusId;

    public GlamourerBonus Clone() => (GlamourerBonus)MemberwiseClone();

    public bool IsEqualTo(GlamourerBonus other)
    {
        if (BonusId != other.BonusId) return false;
        if (Apply != other.Apply) return false;
        return true;
    }
}
