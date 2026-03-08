using System;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerPercentage
{
    private const float Tolerance = 1e-5f;

    [Key(0)]
    public bool Apply;

    [Key(1)]
    public float Percentage;

    public GlamourerPercentage Clone() => (GlamourerPercentage)MemberwiseClone();

    public bool IsEqualTo(GlamourerPercentage other)
    {
        if (Apply != other.Apply) return false;
        if (Math.Abs(Percentage - other.Percentage) > Tolerance) return false;
        return true;
    }
}
