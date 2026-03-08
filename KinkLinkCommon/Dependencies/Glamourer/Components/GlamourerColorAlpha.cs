using System;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerColorAlpha
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

    [Key(4)]
    public float Alpha;

    public GlamourerColorAlpha Clone() => (GlamourerColorAlpha)MemberwiseClone();

    public bool IsEqualTo(GlamourerColorAlpha other)
    {
        if (Apply != other.Apply) return false;
        if (Math.Abs(Red - other.Red) > Tolerance) return false;
        if (Math.Abs(Green - other.Green) > Tolerance) return false;
        if (Math.Abs(Blue - other.Blue) > Tolerance) return false;
        if (Math.Abs(Alpha - other.Alpha) > Tolerance) return false;
        return true;
    }
}
