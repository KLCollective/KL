using System.Text.Json.Serialization;

namespace KinkLinkServer.Domain;

public record ProfileChangeEvent(
    [property: JsonPropertyName("profile_id")] int ProfileId,
    [property: JsonPropertyName("action")] string Action
);

public record LockChangeEvent(
    [property: JsonPropertyName("lock_id")] string LockId,
    [property: JsonPropertyName("lockee_id")] int LockeeId,
    [property: JsonPropertyName("locker_id")] int LockerId,
    [property: JsonPropertyName("action")] string Action
);
