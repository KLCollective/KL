using KinkLinkCommon.Domain;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Locks;

[MessagePackObject]
public record AddLockRequest([property: Key(0)] LockInfoDto LockInfo);

[MessagePackObject]
public record RemoveLockRequest(
    [property: Key(0)] LockKind LockId,
    [property: Key(1)] string LockeeUid
);
