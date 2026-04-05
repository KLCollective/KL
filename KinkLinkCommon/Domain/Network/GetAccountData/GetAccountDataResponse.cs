using KinkLinkCommon.Domain.Network.PairInteractions;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.GetAccountData;

[MessagePackObject]
public record GetAccountDataResponse(
    [property: Key(0)] GetAccountDataEc Result,
    [property: Key(1)] string FriendCode,
    [property: Key(2)] List<FriendRelationship> Relationships,
    [property: Key(3)] Dictionary<string, QueryPairStateResponse> PairStates
);
