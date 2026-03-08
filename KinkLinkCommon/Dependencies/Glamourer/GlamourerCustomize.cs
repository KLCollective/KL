using KinkLinkCommon.Dependencies.Glamourer.Components;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer;

[MessagePackObject]
public class GlamourerCustomize
{
    [Key(0)]
    public uint ModelId;

    [Key(1)]
    public GlamourerValue BodyType = new();

    [Key(2)]
    public GlamourerValue BustSize = new();

    [Key(3)]
    public GlamourerValue Clan = new();

    [Key(4)]
    public GlamourerValue Eyebrows = new();

    [Key(5)]
    public GlamourerValue EyeColorLeft = new();

    [Key(6)]
    public GlamourerValue EyeColorRight = new();

    [Key(7)]
    public GlamourerValue EyeShape = new();

    [Key(8)]
    public GlamourerValue Face = new();

    [Key(9)]
    public GlamourerValue FacePaint = new();

    [Key(10)]
    public GlamourerValue FacePaintColor = new();

    [Key(11)]
    public GlamourerValue FacePaintReversed = new();

    [Key(12)]
    public GlamourerValue FacialFeature1 = new();

    [Key(13)]
    public GlamourerValue FacialFeature2 = new();

    [Key(14)]
    public GlamourerValue FacialFeature3 = new();

    [Key(15)]
    public GlamourerValue FacialFeature4 = new();

    [Key(16)]
    public GlamourerValue FacialFeature5 = new();

    [Key(17)]
    public GlamourerValue FacialFeature6 = new();

    [Key(18)]
    public GlamourerValue FacialFeature7 = new();

    [Key(19)]
    public GlamourerValue Gender = new();

    [Key(20)]
    public GlamourerValue HairColor = new();

    [Key(21)]
    public GlamourerValue Hairstyle = new();

    [Key(22)]
    public GlamourerValue Height = new();

    [Key(23)]
    public GlamourerValue Highlights = new();

    [Key(24)]
    public GlamourerValue HighlightsColor = new();

    [Key(25)]
    public GlamourerValue Jaw = new();

    [Key(26)]
    public GlamourerValue LegacyTattoo = new();

    [Key(27)]
    public GlamourerValue LipColor = new();

    [Key(28)]
    public GlamourerValue Lipstick = new();

    [Key(29)]
    public GlamourerValue Mouth = new();

    [Key(30)]
    public GlamourerValue MuscleMass = new();

    [Key(31)]
    public GlamourerValue Nose = new();

    [Key(32)]
    public GlamourerValue Race = new();

    [Key(33)]
    public GlamourerValue SkinColor = new();

    [Key(34)]
    public GlamourerValue SmallIris = new();

    [Key(35)]
    public GlamourerValue TailShape = new();

    [Key(36)]
    public GlamourerValue TattooColor = new();

    [Key(37)]
    public GlamourerValue Wetness = new();

    public GlamourerCustomize Clone()
    {
        var copy = (GlamourerCustomize)MemberwiseClone();
        copy.BodyType = BodyType.Clone();
        copy.BustSize = BustSize.Clone();
        copy.Clan = Clan.Clone();
        copy.Eyebrows = Eyebrows.Clone();
        copy.EyeColorLeft = EyeColorLeft.Clone();
        copy.EyeColorRight = EyeColorRight.Clone();
        copy.EyeShape = EyeShape.Clone();
        copy.Face = Face.Clone();
        copy.FacePaint = FacePaint.Clone();
        copy.FacePaintColor = FacePaintColor.Clone();
        copy.FacePaintReversed = FacePaintReversed.Clone();
        copy.FacialFeature1 = FacialFeature1.Clone();
        copy.FacialFeature2 = FacialFeature2.Clone();
        copy.FacialFeature3 = FacialFeature3.Clone();
        copy.FacialFeature4 = FacialFeature4.Clone();
        copy.FacialFeature5 = FacialFeature5.Clone();
        copy.FacialFeature6 = FacialFeature6.Clone();
        copy.FacialFeature7 = FacialFeature7.Clone();
        copy.Gender = Gender.Clone();
        copy.HairColor = HairColor.Clone();
        copy.Hairstyle = Hairstyle.Clone();
        copy.Height = Height.Clone();
        copy.Highlights = Highlights.Clone();
        copy.HighlightsColor = HighlightsColor.Clone();
        copy.Jaw = Jaw.Clone();
        copy.LegacyTattoo = LegacyTattoo.Clone();
        copy.LipColor = LipColor.Clone();
        copy.Lipstick = Lipstick.Clone();
        copy.Mouth = Mouth.Clone();
        copy.MuscleMass = MuscleMass.Clone();
        copy.Nose = Nose.Clone();
        copy.Race = Race.Clone();
        copy.SkinColor = SkinColor.Clone();
        copy.SmallIris = SmallIris.Clone();
        copy.TailShape = TailShape.Clone();
        copy.TattooColor = TattooColor.Clone();
        copy.Wetness = Wetness.Clone();
        return copy;
    }
}
