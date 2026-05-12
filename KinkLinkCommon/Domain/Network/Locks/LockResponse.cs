using MessagePack;

namespace KinkLinkCommon.Domain.Network.Locks;

[MessagePackObject]
public record SyncLocksResponse([property: Key(0)] List<LockInfoDto> Locks)
{
    public SyncLocksResponse() : this([]) { }
}

[MessagePackObject]
public record AddLockResponse([property: Key(0)] LockInfoDto LockInfo);

[MessagePackObject]
public record RemoveLockResponse([property: Key(0)] bool Success);
