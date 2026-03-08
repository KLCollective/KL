using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerIsToggled
{
    [Key(0)]
    public bool Apply;

    [Key(1)]
    public bool IsToggled;

    public GlamourerIsToggled Clone() => (GlamourerIsToggled)MemberwiseClone();

    public bool IsEqualTo(GlamourerIsToggled other)
    {
        if (Apply != other.Apply) return false;
        if (IsToggled != other.IsToggled) return false;
        return true;
    }
}
