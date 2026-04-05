using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.Domain;

public record SlotLockInfo(
    string SlotName,
    bool IsLocked,
    string? LockedByName,
    RelationshipPriority Priority
);
