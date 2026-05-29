using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network.PairInteractions;
using MessagePack;

namespace KinkLinkCommonTests.Serialization;

public class PairInteractionsNetworkSerializationTests
{
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);

    #region QueryPairStateRequest Tests

    [Fact]
    public void QueryPairStateRequest_RoundTrip_PreservesFriendCode()
    {
        var request = new QueryPairStateRequest("ABCDEFGH");

        var data = MessagePackSerializer.Serialize(request, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<QueryPairStateRequest>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("ABCDEFGH", deserialized.TargetFriendCode);
    }

    #endregion

    #region QueryPairWardrobeRequest Tests

    [Fact]
    public void QueryPairWardrobeRequest_RoundTrip_PreservesFriendCode()
    {
        var request = new QueryPairWardrobeRequest("TESTCODE");

        var data = MessagePackSerializer.Serialize(request, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<QueryPairWardrobeRequest>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("TESTCODE", deserialized.TargetFriendCode);
    }

    #endregion
}