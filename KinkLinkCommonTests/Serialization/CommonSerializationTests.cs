using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;
using MessagePack.Resolvers;
using Xunit;

namespace KinkLinkCommonTests.Serialization;

public class CommonSerializationTests
{
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions
            .Standard.WithSecurity(MessagePackSecurity.UntrustedData)
            .WithResolver(ContractlessStandardResolver.Instance);

    private static byte[] Serialize<T>(T obj) =>
        MessagePackSerializer.Serialize(obj, MessagePackOptions);

    private static T Deserialize<T>(byte[] data) =>
        MessagePackSerializer.Deserialize<T>(data, MessagePackOptions)!;

    private static byte[] InvalidMessagePack => new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };

    #region Domain Models Tests

    public class DomainModelsTests
    {
        [Fact]
        public void SerializableVector3_RoundTrip_PreservesValues()
        {
            var original = new SerializableVector3
            {
                X = 1.5f,
                Y = 2.5f,
                Z = 3.5f,
            };
            var data = Serialize(original);
            var deserialized = Deserialize<SerializableVector3>(data);
            Assert.Equal(1.5f, deserialized.X);
            Assert.Equal(2.5f, deserialized.Y);
            Assert.Equal(3.5f, deserialized.Z);
        }

        [Fact]
        public void SerializableVector3_ZeroValues_RoundTripsCorrectly()
        {
            var original = new SerializableVector3
            {
                X = 0,
                Y = 0,
                Z = 0,
            };
            var data = Serialize(original);
            var deserialized = Deserialize<SerializableVector3>(data);
            Assert.Equal(0f, deserialized.X);
            Assert.Equal(0f, deserialized.Y);
            Assert.Equal(0f, deserialized.Z);
        }

        [Fact]
        public void SerializableVector4_RoundTrip_PreservesValues()
        {
            var original = new SerializableVector4
            {
                X = 1.5f,
                Y = 2.5f,
                Z = 3.5f,
                W = 4.5f,
            };
            var data = Serialize(original);
            var deserialized = Deserialize<SerializableVector4>(data);
            Assert.Equal(1.5f, deserialized.X);
            Assert.Equal(2.5f, deserialized.Y);
            Assert.Equal(3.5f, deserialized.Z);
            Assert.Equal(4.5f, deserialized.W);
        }

        [Fact]
        public void GagStateDto_RoundTrip_PreservesAllData()
        {
            var original = new GagStateDto(true, true, "ball-gag", true);
            var data = Serialize(original);
            var deserialized = Deserialize<GagStateDto>(data);
            Assert.True(deserialized.IsEnabled);
            Assert.True(deserialized.IsLocked);
            Assert.Equal("ball-gag", deserialized.GagType);
            Assert.True(deserialized.IsGlamourEnabled);
        }

        [Fact]
        public void GagStateDto_Disabled_RoundTripsCorrectly()
        {
            var original = new GagStateDto(false, false, null, false);
            var data = Serialize(original);
            var deserialized = Deserialize<GagStateDto>(data);
            Assert.False(deserialized.IsEnabled);
            Assert.False(deserialized.IsLocked);
            Assert.Null(deserialized.GagType);
            Assert.False(deserialized.IsGlamourEnabled);
        }

        [Fact]
        public void GarblerStateDto_RoundTrip_PreservesAllData()
        {
            var original = new GarblerStateDto(true, true, 7);
            var data = Serialize(original);
            var deserialized = Deserialize<GarblerStateDto>(data);
            Assert.True(deserialized.IsEnabled);
            Assert.True(deserialized.IsLocked);
            Assert.Equal(7, deserialized.EnabledChannels);
        }

        [Fact]
        public void GarblerStateDto_Disabled_RoundTripsCorrectly()
        {
            var original = new GarblerStateDto(false, false, 0);
            var data = Serialize(original);
            var deserialized = Deserialize<GarblerStateDto>(data);
            Assert.False(deserialized.IsEnabled);
            Assert.False(deserialized.IsLocked);
            Assert.Equal(0, deserialized.EnabledChannels);
        }

        [Fact]
        public void UserGarblerSettings_RoundTrip_PreservesAllData()
        {
            var original = new UserGarblerSettings(
                true,
                true,
                true,
                GarblerChannels.Say | GarblerChannels.Shout
            );
            var data = Serialize(original);
            var deserialized = Deserialize<UserGarblerSettings>(data);
            Assert.True(deserialized.GarblerEnabled);
            Assert.True(deserialized.GarblerLocked);
            Assert.True(deserialized.GarblerChannelsLocked);
            Assert.Equal(GarblerChannels.Say | GarblerChannels.Shout, deserialized.Channels);
        }

        [Fact]
        public void UserGarblerSettings_Default_RoundTripsCorrectly()
        {
            var original = new UserGarblerSettings();
            var data = Serialize(original);
            var deserialized = Deserialize<UserGarblerSettings>(data);
            Assert.False(deserialized.GarblerEnabled);
            Assert.False(deserialized.GarblerLocked);
            Assert.False(deserialized.GarblerChannelsLocked);
            Assert.Equal(GarblerChannels.None, deserialized.Channels);
        }

        [Fact]
        public void WardrobeDto_RoundTrip_PreservesAllData()
        {
            var original = new WardrobeDto(
                Guid.NewGuid(),
                "Test Outfit",
                "A test outfit",
                "body",
                GlamourerEquipmentSlot.Body,
                "base64data",
                RelationshipPriority.Serious,
                "lock-123"
            );
            var data = Serialize(original);
            var deserialized = Deserialize<WardrobeDto>(data);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.Description, deserialized.Description);
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.Slot, deserialized.Slot);
            Assert.Equal(original.DataBase64, deserialized.DataBase64);
            Assert.Equal(original.Priority, deserialized.Priority);
            Assert.Equal(original.LockId, deserialized.LockId);
        }

        [Fact]
        public void WardrobeDto_NullLockId_RoundTripsCorrectly()
        {
            var original = new WardrobeDto(
                Guid.NewGuid(),
                "Test",
                "",
                "head",
                GlamourerEquipmentSlot.Head,
                "",
                RelationshipPriority.Casual,
                null
            );
            var data = Serialize(original);
            var deserialized = Deserialize<WardrobeDto>(data);
            Assert.Null(deserialized.LockId);
        }

        [Fact]
        public void WardrobeItemData_RoundTrip_PreservesAllData()
        {
            var original = new WardrobeItemData(
                Guid.NewGuid(),
                "Head Gear",
                "A nice headpiece",
                GlamourerEquipmentSlot.Head,
                new GlamourerItem { Apply = true, ItemId = 12345 },
                new List<GlamourerMod>(),
                new Dictionary<string, GlamourerMaterial>(),
                RelationshipPriority.Devotional
            );
            var data = Serialize(original);
            var deserialized = Deserialize<WardrobeItemData>(data);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.Description, deserialized.Description);
            Assert.Equal(original.Slot, deserialized.Slot);
            Assert.Equal(original.Priority, deserialized.Priority);
        }

        [Fact]
        public void WardrobeStateDto_RoundTrip_PreservesAllData()
        {
            var original = new WardrobeStateDto(
                "base64layer",
                new Dictionary<string, WardrobeItemData>
                {
                    ["head"] = new WardrobeItemData(
                        Guid.NewGuid(),
                        "Head",
                        "",
                        GlamourerEquipmentSlot.Head,
                        null,
                        null,
                        null,
                        RelationshipPriority.Casual
                    ),
                    ["body"] = new WardrobeItemData(
                        Guid.NewGuid(),
                        "Body",
                        "",
                        GlamourerEquipmentSlot.Body,
                        null,
                        null,
                        null,
                        RelationshipPriority.Serious
                    ),
                },
                null
            );
            var data = Serialize(original);
            var deserialized = Deserialize<WardrobeStateDto>(data);
            Assert.Equal(original.BaseLayerBase64, deserialized.BaseLayerBase64);
            Assert.Equal(2, deserialized.Equipment?.Count);
        }

        [Fact]
        public void PairWardrobeItemDto_RoundTrip_PreservesAllData()
        {
            var original = new PairWardrobeItemDto(
                Guid.NewGuid(),
                "Pair Outfit",
                "Shared outfit",
                GlamourerEquipmentSlot.Legs,
                RelationshipPriority.Serious,
                "lock-456"
            );
            var data = Serialize(original);
            var deserialized = Deserialize<PairWardrobeItemDto>(data);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.Description, deserialized.Description);
            Assert.Equal(original.Slot, deserialized.Slot);
            Assert.Equal(original.Priority, deserialized.Priority);
            Assert.Equal(original.LockId, deserialized.LockId);
        }

        [Fact]
        public void PairWardrobeStateDto_RoundTrip_PreservesAllData()
        {
            var original = new PairWardrobeStateDto(
                new PairWardrobeItemDto(
                    Guid.NewGuid(),
                    "Base",
                    "",
                    GlamourerEquipmentSlot.Body,
                    RelationshipPriority.Casual,
                    null
                ),
                new Dictionary<string, PairWardrobeItemDto>
                {
                    ["head"] = new PairWardrobeItemDto(
                        Guid.NewGuid(),
                        "Head",
                        "",
                        GlamourerEquipmentSlot.Head,
                        RelationshipPriority.Serious,
                        "lock-1"
                    ),
                }
            );
            var data = Serialize(original);
            var deserialized = Deserialize<PairWardrobeStateDto>(data);
            Assert.NotNull(deserialized.BaseLayer);
            Assert.Single(deserialized.Equipment!);
        }

        [Fact]
        public void UserPermissions_RoundTrip_PreservesAllData()
        {
            var original = new UserPermissions(
                "test-uid",
                DateTime.UtcNow.AddDays(1),
                RelationshipPriority.Devotional,
                true,
                true,
                false,
                (int)(InteractionPerms.CanApplyGag | InteractionPerms.CanLockGag)
            );
            var data = Serialize(original);
            var deserialized = Deserialize<UserPermissions>(data);
            Assert.Equal(original.PairUid, deserialized.PairUid);
            Assert.Equal(original.Priority, deserialized.Priority);
            Assert.Equal(original.ControlsPerm, deserialized.ControlsPerm);
            Assert.Equal(original.ControlsConfig, deserialized.ControlsConfig);
            Assert.Equal(original.DisableSafeword, deserialized.DisableSafeword);
            Assert.Equal(original.Perms, deserialized.Perms);
        }
    }

    #endregion

    #region Network Action Types Tests

    public class ActionTypesTests
    {
        [Fact]
        public void ActionCommand_RoundTrip_PreservesData()
        {
            var original = new ActionCommand("TARGET-FC-123");
            var data = Serialize(original);
            var deserialized = Deserialize<ActionCommand>(data);
            Assert.Equal("TARGET-FC-123", deserialized.TargetFriendCode);
        }

        [Fact]
        public void ActionRequest_SingleTarget_RoundTripsCorrectly()
        {
            var original = new ActionRequest(new List<string> { "FC-001" });
            var data = Serialize(original);
            var deserialized = Deserialize<ActionRequest>(data);
            Assert.Single(deserialized.TargetFriendCodes);
            Assert.Equal("FC-001", deserialized.TargetFriendCodes[0]);
        }

        [Fact]
        public void ActionRequest_MultipleTargets_RoundTripsCorrectly()
        {
            var original = new ActionRequest(new List<string> { "FC-001", "FC-002", "FC-003" });
            var data = Serialize(original);
            var deserialized = Deserialize<ActionRequest>(data);
            Assert.Equal(3, deserialized.TargetFriendCodes.Count);
        }

        [Fact]
        public void ActionRequest_EmptyList_RoundTripsCorrectly()
        {
            var original = new ActionRequest(new List<string>());
            var data = Serialize(original);
            var deserialized = Deserialize<ActionRequest>(data);
            Assert.Empty(deserialized.TargetFriendCodes);
        }

        [Fact]
        public void ActionResponse_Success_RoundTripsCorrectly()
        {
            var original = new ActionResponse(
                ActionResponseEc.Success,
                new Dictionary<string, ActionResultEc>()
            );
            var data = Serialize(original);
            var deserialized = Deserialize<ActionResponse>(data);
            Assert.Equal(ActionResponseEc.Success, deserialized.Result);
            Assert.Empty(deserialized.Results);
        }

        [Fact]
        public void ActionResponse_WithResults_RoundTripsCorrectly()
        {
            var results = new Dictionary<string, ActionResultEc>
            {
                ["FC-001"] = ActionResultEc.Success,
                ["FC-002"] = ActionResultEc.Unknown,
            };
            var original = new ActionResponse(ActionResponseEc.Success, results);
            var data = Serialize(original);
            var deserialized = Deserialize<ActionResponse>(data);
            Assert.Equal(ActionResponseEc.Success, deserialized.Result);
            Assert.Equal(2, deserialized.Results.Count);
        }

        [Fact]
        public void ActionResult_Success_WithValue_RoundTripsCorrectly()
        {
            var original = new ActionResult<string>(ActionResultEc.Success, "test-value");
            var data = Serialize(original);
            var deserialized = Deserialize<ActionResult<string>>(data);
            Assert.Equal(ActionResultEc.Success, deserialized.Result);
            Assert.Equal("test-value", deserialized.Value);
        }

        [Fact]
        public void ActionResult_Failure_NullValue_RoundTripsCorrectly()
        {
            var original = new ActionResult<string>(ActionResultEc.Unknown, null);
            var data = Serialize(original);
            var deserialized = Deserialize<ActionResult<string>>(data);
            Assert.Equal(ActionResultEc.Unknown, deserialized.Result);
            Assert.Null(deserialized.Value);
        }
    }

    #endregion

    #region Speak/Emote Tests

    public class NetworkCommandsTests
    {
        // Note: SpeakCommand, EmoteCommand, and ApplyInteractionCommand have [MessagePackObject] without
        // keyAsPropertyName: true and without explicit Key attributes - they cannot be serialized properly
        // with ContractlessStandardResolver. These types appear to have a serialization bug.
    }

    #endregion

    #region Pair Interactions Tests

    public class PairInteractionsTests
    {
        [Fact]
        public void QueryPairStateRequest_RoundTrip_PreservesData()
        {
            var original = new QueryPairStateRequest("PARTNER-FC");
            var data = Serialize(original);
            var deserialized = Deserialize<QueryPairStateRequest>(data);
            Assert.Equal("PARTNER-FC", deserialized.TargetFriendCode);
        }

        [Fact]
        public void QueryPairStateResponse_RoundTrip_PreservesData()
        {
            var perms = new UserPermissions(
                "uid",
                DateTime.UtcNow.AddDays(1),
                RelationshipPriority.Casual,
                false,
                false,
                false,
                0
            );
            var wardrobe = new PairWardrobeStateDto(null, null);
            var locks = new List<LockInfoDto>();

            var original = new QueryPairStateResponse("PARTNER-FC", perms, wardrobe, locks);
            var data = Serialize(original);
            var deserialized = Deserialize<QueryPairStateResponse>(data);

            Assert.Equal("PARTNER-FC", deserialized.TargetFriendCode);
            Assert.NotNull(deserialized.GrantedTo);
            Assert.NotNull(deserialized.WardrobeState);
            Assert.Empty(deserialized.LockStates);
        }

        // Note: ApplyInteractionCommand has [MessagePackObject] without keyAsPropertyName: true and without
        // explicit Key attributes - cannot be serialized properly with ContractlessStandardResolver.

        [Fact]
        public void InteractionPayload_RoundTrip_PreservesData()
        {
            var gag = new GagStateDto(true, false, "ball", false);
            var original = new InteractionPayload(gag, null, null, null);
            var data = Serialize(original);
            var deserialized = Deserialize<InteractionPayload>(data);

            Assert.NotNull(deserialized.Gag);
            Assert.True(deserialized.Gag!.IsEnabled);
            Assert.Equal("ball", deserialized.Gag.GagType);
        }

        [Fact]
        public void QueryPairWardrobeStateRequest_RoundTrip_PreservesData()
        {
            var original = new QueryPairWardrobeStateRequest("PARTNER-FC");
            var data = Serialize(original);
            var deserialized = Deserialize<QueryPairWardrobeStateRequest>(data);
            Assert.Equal("PARTNER-FC", deserialized.TargetFriendCode);
        }

        [Fact]
        public void QueryPairWardrobeStateResponse_RoundTrip_PreservesData()
        {
            var original = new QueryPairWardrobeStateResponse("PARTNER-FC", true, null);
            var data = Serialize(original);
            var deserialized = Deserialize<QueryPairWardrobeStateResponse>(data);
            Assert.Equal("PARTNER-FC", deserialized.TargetFriendCode);
            Assert.True(deserialized.HasWardrobePermission);
        }

        [Fact]
        public void QueryPairWardrobeRequest_RoundTrip_PreservesData()
        {
            var original = new QueryPairWardrobeRequest("PARTNER-FC");
            var data = Serialize(original);
            var deserialized = Deserialize<QueryPairWardrobeRequest>(data);
            Assert.Equal("PARTNER-FC", deserialized.TargetFriendCode);
        }

        [Fact]
        public void QueryPairWardrobeResponse_RoundTrip_PreservesData()
        {
            var items = new List<PairWardrobeItemDto>
            {
                new(
                    Guid.NewGuid(),
                    "Item1",
                    "",
                    GlamourerEquipmentSlot.Head,
                    RelationshipPriority.Casual,
                    null
                ),
                new(
                    Guid.NewGuid(),
                    "Item2",
                    "",
                    GlamourerEquipmentSlot.Body,
                    RelationshipPriority.Serious,
                    null
                ),
            };
            var original = new QueryPairWardrobeResponse("PARTNER-FC", items);
            var data = Serialize(original);
            var deserialized = Deserialize<QueryPairWardrobeResponse>(data);

            Assert.Equal("PARTNER-FC", deserialized.TargetFriendCode);
            Assert.Equal(2, deserialized.Items.Count);
        }
    }

    #endregion

    #region Glamourer Components Tests

    public class GlamourerComponentsTests
    {
        [Fact]
        public void GlamourerMaterial_RoundTrip_PreservesData()
        {
            var original = new GlamourerMaterial
            {
                Enabled = true,
                Revert = false,
                Gloss = 0.5f,
                DiffuseR = 0.8f,
                DiffuseG = 0.2f,
                DiffuseB = 0.1f,
                EmissiveR = 0f,
                EmissiveG = 0f,
                EmissiveB = 0f,
                SpecularR = 1f,
                SpecularG = 1f,
                SpecularB = 1f,
                SpecularA = 0.5f,
            };
            var data = Serialize(original);
            var deserialized = Deserialize<GlamourerMaterial>(data);
            Assert.True(deserialized.Enabled);
            Assert.Equal(0.5f, deserialized.Gloss);
        }

        [Fact]
        public void GlamourerValue_RoundTrip_PreservesData()
        {
            var original = new GlamourerValue { Apply = true, Value = 100u };
            var data = Serialize(original);
            var deserialized = Deserialize<GlamourerValue>(data);
            Assert.True(deserialized.Apply);
            Assert.Equal(100u, deserialized.Value);
        }

        [Fact]
        public void GlamourerBonus_RoundTrip_PreservesData()
        {
            var original = new GlamourerBonus { Apply = true, BonusId = 5uL };
            var data = Serialize(original);
            var deserialized = Deserialize<GlamourerBonus>(data);
            Assert.True(deserialized.Apply);
            Assert.Equal(5uL, deserialized.BonusId);
        }
    }

    #endregion

    #region Invalid Data Tests

    public class InvalidDataTests
    {
        [Fact]
        public void Invalid_MessagePack_ThrowsForGagStateDto()
        {
            Assert.Throws<MessagePackSerializationException>(() =>
                Deserialize<GagStateDto>(InvalidMessagePack)
            );
        }

        [Fact]
        public void Invalid_MessagePack_ThrowsForGarblerStateDto()
        {
            Assert.Throws<MessagePackSerializationException>(() =>
                Deserialize<GarblerStateDto>(InvalidMessagePack)
            );
        }

        [Fact]
        public void Invalid_MessagePack_ThrowsForSerializableVector3()
        {
            Assert.Throws<MessagePackSerializationException>(() =>
                Deserialize<SerializableVector3>(InvalidMessagePack)
            );
        }
    }

    #endregion
}

