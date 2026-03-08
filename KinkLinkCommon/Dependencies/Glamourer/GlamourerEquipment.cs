using KinkLinkCommon.Dependencies.Glamourer.Components;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer;

[MessagePackObject]
public class GlamourerEquipment
{
    [Key(0)]
    public GlamourerItem MainHand = new();

    [Key(1)]
    public GlamourerItem OffHand = new();

    [Key(2)]
    public GlamourerItem Head = new();

    [Key(3)]
    public GlamourerItem Body = new();

    [Key(4)]
    public GlamourerItem Hands = new();

    [Key(5)]
    public GlamourerItem Legs = new();

    [Key(6)]
    public GlamourerItem Feet = new();

    [Key(7)]
    public GlamourerItem Ears = new();

    [Key(8)]
    public GlamourerItem Neck = new();

    [Key(9)]
    public GlamourerItem Wrists = new();

    [Key(10)]
    public GlamourerItem RFinger = new();

    [Key(11)]
    public GlamourerItem LFinger = new();

    [Key(12)]
    public GlamourerShow Hat = new();

    [Key(13)]
    public GlamourerShow VieraEars = new();

    [Key(14)]
    public GlamourerShow Weapon = new();

    [Key(15)]
    public GlamourerIsToggled Visor = new();

    public override string ToString()
    {
        return $"MainHand: {MainHand.ItemId}, OffHand: {OffHand.ItemId}, Head: {Head.ItemId}, Body: {Body.ItemId}, Hands: {Hands.ItemId}, Legs: {Legs.ItemId}, Feet: {Feet.ItemId}, Ears: {Ears.ItemId}, Neck: {Neck.ItemId}, Wrists: {Wrists.ItemId}, RFinger: {RFinger.ItemId}, LFinger: {LFinger.ItemId}, Hat: {Hat}, VieraEars: {VieraEars}, Weapon: {Weapon}, Visor: {Visor}";
    }

    public GlamourerEquipment Clone()
    {
        var copy = (GlamourerEquipment)MemberwiseClone();
        copy.MainHand = MainHand.Clone();
        copy.OffHand = OffHand.Clone();
        copy.Head = Head.Clone();
        copy.Body = Body.Clone();
        copy.Hands = Hands.Clone();
        copy.Legs = Legs.Clone();
        copy.Feet = Feet.Clone();
        copy.Ears = Ears.Clone();
        copy.Neck = Neck.Clone();
        copy.Wrists = Wrists.Clone();
        copy.RFinger = RFinger.Clone();
        copy.LFinger = LFinger.Clone();
        copy.Hat = Hat.Clone();
        copy.VieraEars = VieraEars.Clone();
        copy.Weapon = Weapon.Clone();
        copy.Visor = Visor.Clone();
        return copy;
    }
}
