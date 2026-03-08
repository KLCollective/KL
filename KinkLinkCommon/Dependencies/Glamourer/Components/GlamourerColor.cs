using System;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerColor
{
    private const float Tolerance = 1e-5f;

    [Key(0)]
    public bool Apply;

    [Key(1)]
    public float Red;

    [Key(2)]
    public float Green;

    [Key(3)]
    public float Blue;

    public GlamourerColor Clone() => (GlamourerColor)MemberwiseClone();

    public bool IsEqualTo(GlamourerColor other)
    {
        if (Apply != other.Apply) return false;
        if (Math.Abs(Red - other.Red) > Tolerance) return false;
        if (Math.Abs(Green - other.Green) > Tolerance) return false;
        if (Math.Abs(Blue - other.Blue) > Tolerance) return false;
        return true;
    }
}
