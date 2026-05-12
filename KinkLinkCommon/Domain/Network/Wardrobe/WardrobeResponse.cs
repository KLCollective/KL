using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Wardrobe;

[MessagePackObject]
public record AddWardrobeItemResponse([property: Key(0)] WardrobeDto Item);

[MessagePackObject]
public record RemoveWardrobeItemResponse([property: Key(0)] bool Success);

[MessagePackObject]
public record GetWardrobeItemResponse([property: Key(0)] WardrobeDto? Item);

[MessagePackObject]
public record ListWardrobeItemsResponse([property: Key(0)] List<WardrobeDto> Items)
{
    public ListWardrobeItemsResponse() : this([]) { }
}

[MessagePackObject]
public record SetWardrobeStatusResponse([property: Key(0)] bool Success);

[MessagePackObject]
public record GetWardrobeStatusResponse([property: Key(0)] WardrobeStateDto? State);
