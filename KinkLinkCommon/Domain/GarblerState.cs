using MessagePack;

namespace KinkLinkCommon.Domain.CharacterState;

[MessagePackObject]
public record GagStateDto(
    [property: Key(0)] bool IsEnabled,
    [property: Key(1)] bool IsLocked,
    [property: Key(2)] string? GagType,
    [property: Key(3)] bool IsGlamourEnabled
);

[MessagePackObject]
public record GarblerStateDto(
    [property: Key(0)] bool IsEnabled,
    [property: Key(1)] bool IsLocked,
    [property: Key(2)] int EnabledChannels
);
