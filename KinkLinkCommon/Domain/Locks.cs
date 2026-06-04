using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;

namespace KinkLinkCommon.Domain;

public enum LockKind
{
    WardrobeHead,
    WardrobeChest,
    WardrobeHands,
    WardrobeLegs,
    WardrobeFeet,
    WardrobeEars,
    WardrobeNeck,
    WardrobeWrists,
    WardrobeRFinger,
    WardrobeLFinger,
    WardrobeMods,
    WardrobeOutfit1,
}

public static class LockKindExtensions
{
    public static LockKind From(WardrobeLayer layer)
    {
        switch (layer)
        {
            case WardrobeLayer.Head:
                return LockKind.WardrobeHead;
            case WardrobeLayer.Chest:
                return LockKind.WardrobeChest;
            case WardrobeLayer.Hands:
                return LockKind.WardrobeHands;
            case WardrobeLayer.Legs:
                return LockKind.WardrobeLegs;
            case WardrobeLayer.Feet:
                return LockKind.WardrobeFeet;
            case WardrobeLayer.Ears:
                return LockKind.WardrobeEars;
            case WardrobeLayer.Neck:
                return LockKind.WardrobeNeck;
            case WardrobeLayer.Wrists:
                return LockKind.WardrobeWrists;
            case WardrobeLayer.RFinger:
                return LockKind.WardrobeRFinger;
            case WardrobeLayer.LFinger:
                return LockKind.WardrobeLFinger;
            case WardrobeLayer.Mods:
                return LockKind.WardrobeMods;
            case WardrobeLayer.Outfit:
                return LockKind.WardrobeOutfit1;
            default:
                throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static WardrobeLayer ToWardrobeLayer(this LockKind kind)
    {
        switch (kind)
        {
            case LockKind.WardrobeHead:
                return WardrobeLayer.Head;
            case LockKind.WardrobeChest:
                return WardrobeLayer.Chest;
            case LockKind.WardrobeHands:
                return WardrobeLayer.Hands;
            case LockKind.WardrobeLegs:
                return WardrobeLayer.Legs;
            case LockKind.WardrobeFeet:
                return WardrobeLayer.Feet;
            case LockKind.WardrobeEars:
                return WardrobeLayer.Ears;
            case LockKind.WardrobeNeck:
                return WardrobeLayer.Neck;
            case LockKind.WardrobeWrists:
                return WardrobeLayer.Wrists;
            case LockKind.WardrobeRFinger:
                return WardrobeLayer.RFinger;
            case LockKind.WardrobeLFinger:
                return WardrobeLayer.LFinger;
            case LockKind.WardrobeMods:
                return WardrobeLayer.Mods;
            case LockKind.WardrobeOutfit1:
                return WardrobeLayer.Outfit;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}

[MessagePackObject]
public struct LockInfoDto
{
    [Key(0)]
    public LockKind LockID;

    [Key(1)]
    public string LockeeID;

    [Key(2)]
    public string LockerID;

    [Key(3)]
    public RelationshipPriority LockPriority;

    [Key(4)]
    public bool CanSelfUnlock;

    [Key(5)]
    public DateTime? Expires;

    [Key(6)]
    public string? Password;
}

public class Locks
{
    public static bool CanUnlock(
        string userId,
        LockInfoDto lockInfo,
        RelationshipPriority userPriority,
        string? providedPassword = null
    )
    {
        if (lockInfo.CanSelfUnlock && userId == lockInfo.LockeeID)
        {
            return true;
        }

        if (userPriority >= lockInfo.LockPriority)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(lockInfo.Password) && lockInfo.Password == providedPassword)
        {
            return true;
        }

        return false;
    }
}
