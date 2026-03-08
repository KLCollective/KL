using KinkLinkCommon.Dependencies.Glamourer.Components;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer;

[MessagePackObject]
public class GlamourerParameter
{
    [Key(0)]
    public GlamourerColor FeatureColor = new();

    [Key(1)]
    public GlamourerColor HairDiffuse = new();

    [Key(2)]
    public GlamourerColor HairHighlight = new();

    [Key(3)]
    public GlamourerColor LeftEye = new();

    [Key(4)]
    public GlamourerColor RightEye = new();

    [Key(5)]
    public GlamourerColor SkinDiffuse = new();

    [Key(6)]
    public GlamourerColorAlpha DecalColor = new();

    [Key(7)]
    public GlamourerColorAlpha LipDiffuse = new();

    [Key(8)]
    public GlamourerPercentage FacePaintUvMultiplier = new();

    [Key(9)]
    public GlamourerPercentage FacePaintUvOffset = new();

    [Key(10)]
    public GlamourerPercentage LeftLimbalIntensity = new();

    [Key(11)]
    public GlamourerPercentage RightLimbalIntensity = new();

    [Key(12)]
    public GlamourerPercentage MuscleTone = new();

    public GlamourerParameter Clone()
    {
        var copy = (GlamourerParameter)MemberwiseClone();
        copy.FeatureColor = FeatureColor.Clone();
        copy.HairDiffuse = HairDiffuse.Clone();
        copy.HairHighlight = HairHighlight.Clone();
        copy.LeftEye = LeftEye.Clone();
        copy.RightEye = RightEye.Clone();
        copy.SkinDiffuse = SkinDiffuse.Clone();
        copy.DecalColor = DecalColor.Clone();
        copy.LipDiffuse = LipDiffuse.Clone();
        copy.FacePaintUvMultiplier = FacePaintUvMultiplier.Clone();
        copy.FacePaintUvOffset = FacePaintUvOffset.Clone();
        copy.LeftLimbalIntensity = LeftLimbalIntensity.Clone();
        copy.RightLimbalIntensity = RightLimbalIntensity.Clone();
        copy.MuscleTone = MuscleTone.Clone();
        return copy;
    }

    public bool IsEqualTo(GlamourerParameter other)
    {
        if (FeatureColor.IsEqualTo(other.FeatureColor) is false) return false;
        if (HairDiffuse.IsEqualTo(other.HairDiffuse) is false) return false;
        if (HairHighlight.IsEqualTo(other.HairHighlight) is false) return false;
        if (LeftEye.IsEqualTo(other.LeftEye) is false) return false;
        if (RightEye.IsEqualTo(other.RightEye) is false) return false;
        if (SkinDiffuse.IsEqualTo(other.SkinDiffuse) is false) return false;
        if (DecalColor.IsEqualTo(other.DecalColor) is false) return false;
        if (LipDiffuse.IsEqualTo(other.LipDiffuse) is false) return false;
        if (FacePaintUvMultiplier.IsEqualTo(other.FacePaintUvMultiplier) is false) return false;
        if (FacePaintUvOffset.IsEqualTo(other.FacePaintUvOffset) is false) return false;
        if (LeftLimbalIntensity.IsEqualTo(other.LeftLimbalIntensity) is false) return false;
        if (RightLimbalIntensity.IsEqualTo(other.RightLimbalIntensity) is false) return false;
        if (MuscleTone.IsEqualTo(other.MuscleTone) is false) return false;
        return true;
    }
}
