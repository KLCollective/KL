using System;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerMaterial
{
    private const float Tolerance = 1e-5f;

    [Key(0)]
    public bool Enabled;

    [Key(1)]
    public bool Revert;

    [Key(2)]
    public float Gloss;

    [Key(3)]
    public float DiffuseR;

    [Key(4)]
    public float DiffuseG;

    [Key(5)]
    public float DiffuseB;

    [Key(6)]
    public float EmissiveR;

    [Key(7)]
    public float EmissiveG;

    [Key(8)]
    public float EmissiveB;

    [Key(9)]
    public float SpecularR;

    [Key(10)]
    public float SpecularG;

    [Key(11)]
    public float SpecularB;

    [Key(12)]
    public float SpecularA;

    public GlamourerMaterial Clone() => (GlamourerMaterial)MemberwiseClone();

    public bool IsEqualTo(GlamourerMaterial other)
    {
        if (Enabled != other.Enabled) return false;
        if (Revert != other.Revert) return false;
        if (Math.Abs(Gloss - other.Gloss) > Tolerance) return false;
        if (Math.Abs(DiffuseR - other.DiffuseR) > Tolerance) return false;
        if (Math.Abs(DiffuseG - other.DiffuseG) > Tolerance) return false;
        if (Math.Abs(DiffuseB - other.DiffuseB) > Tolerance) return false;
        if (Math.Abs(EmissiveR - other.EmissiveR) > Tolerance) return false;
        if (Math.Abs(EmissiveG - other.EmissiveG) > Tolerance) return false;
        if (Math.Abs(EmissiveB - other.EmissiveB) > Tolerance) return false;
        if (Math.Abs(SpecularR - other.SpecularR) > Tolerance) return false;
        if (Math.Abs(SpecularG - other.SpecularG) > Tolerance) return false;
        if (Math.Abs(SpecularB - other.SpecularB) > Tolerance) return false;
        if (Math.Abs(SpecularA - other.SpecularA) > Tolerance) return false;
        return true;
    }
}
