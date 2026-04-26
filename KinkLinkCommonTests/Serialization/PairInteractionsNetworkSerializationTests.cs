using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network.PairInteractions;
using MessagePack;

namespace KinkLinkCommonTests.Serialization;

public class PairInteractionsNetworkSerializationTests
{
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);

    #region ApplyInteractionCommand Tests

    [Fact]
    public void ApplyInteractionCommand_Serialize_WithAllFields_Succeeds()
    {
        var command = new ApplyInteractionCommand(
            "TESTCODE",
            PairAction.ApplyWardrobe,
            null
        );

        var data = MessagePackSerializer.Serialize(command, MessagePackOptions);

        Assert.NotEmpty(data);
    }

    [Fact]
    public void ApplyInteractionCommand_RoundTrip_PreservesAllFields()
    {
        var command = new ApplyInteractionCommand(
            "VLHIMNDEER",
            PairAction.ApplyWardrobe,
            null
        );

        var data = MessagePackSerializer.Serialize(command, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<ApplyInteractionCommand>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("VLHIMNDEER", deserialized.TargetFriendCode);
        Assert.Equal(PairAction.ApplyWardrobe, deserialized.Action);
        Assert.Null(deserialized.Payload);
    }

    [Fact]
    public void ApplyInteractionCommand_Serialize_ApplyGag_Succeeds()
    {
        var command = new ApplyInteractionCommand(
            "FRIEND1",
            PairAction.ApplyGag,
            null
        );

        var data = MessagePackSerializer.Serialize(command, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<ApplyInteractionCommand>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(PairAction.ApplyGag, deserialized.Action);
    }

    [Fact]
    public void ApplyInteractionCommand_Serialize_LockGag_Succeeds()
    {
        var command = new ApplyInteractionCommand(
            "FRIEND2",
            PairAction.LockGag,
            null
        );

        var data = MessagePackSerializer.Serialize(command, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<ApplyInteractionCommand>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(PairAction.LockGag, deserialized.Action);
    }

    [Fact]
    public void ApplyInteractionCommand_Serialize_ApplyOwnMoodle_Succeeds()
    {
        var command = new ApplyInteractionCommand(
            "FRIEND3",
            PairAction.ApplyOwnMoodle,
            null
        );

        var data = MessagePackSerializer.Serialize(command, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<ApplyInteractionCommand>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(PairAction.ApplyOwnMoodle, deserialized.Action);
    }

    #endregion

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

    #region InteractionPayload Tests

    [Fact]
    public void InteractionPayload_RoundTrip_WithNullValues_Succeeds()
    {
        var payload = new InteractionPayload(null, null, null, null);

        var data = MessagePackSerializer.Serialize(payload, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<InteractionPayload>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Gag);
        Assert.Null(deserialized.Garbler);
        Assert.Null(deserialized.WardrobeItems);
        Assert.Null(deserialized.Moodle);
    }

    [Fact]
    public void InteractionPayload_RoundTrip_WithWardrobeItems_Succeeds()
    {
        var payload = new InteractionPayload(null, null, [], null);

        var data = MessagePackSerializer.Serialize(payload, MessagePackOptions);
        var deserialized = MessagePackSerializer.Deserialize<InteractionPayload>(data, MessagePackOptions);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.WardrobeItems);
        Assert.Empty(deserialized.WardrobeItems);
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