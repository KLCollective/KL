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
public record SetWardrobeStatusRequest([property: Key(0)] WardrobeStateDto State);

[MessagePackObject]
public record GetWardrobeStatusRequest();

[MessagePackObject]
public record RandomizeActiveWardrobeRequest();
