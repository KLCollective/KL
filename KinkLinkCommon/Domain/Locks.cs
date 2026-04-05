using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain;

[MessagePackObject]
public struct LockInfoDto
{
    [Key(0)] public string LockID;
    [Key(1)] public int LockeeID;
    [Key(2)] public int LockerID;
    [Key(3)] public RelationshipPriority LockPriority;
    [Key(4)] public bool CanSelfUnlock;
    [Key(5)] public DateTime? Expires;
    [Key(6)] public string? Password;
}

public class Locks
{
    public static bool CanUnlock(
        int userId,
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
