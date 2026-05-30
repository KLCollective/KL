using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Wardrobe;

[MessagePackObject]
public record AddWardrobeItemRequest([property: Key(0)] WardrobeDto Item);

[MessagePackObject]
public record RemoveWardrobeItemRequest([property: Key(0)] Guid WardrobeId);

[MessagePackObject]
public record GetWardrobeItemRequest([property: Key(0)] Guid WardrobeId);

[MessagePackObject]
public record ListWardrobeItemsRequest();

[MessagePackObject]
public record GetWardrobeStatusRequest();

[MessagePackObject]
public record SetActiveWardrobeLayerRequest(
    [property: Key(0)] WardrobeLayer Layer,
    [property: Key(1)] WardrobeDto? LayerData
);

[MessagePackObject]
public record RandomizeActiveWardrobeRequest();
