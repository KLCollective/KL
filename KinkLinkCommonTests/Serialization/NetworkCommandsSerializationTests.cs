using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.SyncOnlineStatus;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Network.SyncPermissions;
using MessagePack;
using MessagePack.Resolvers;
using Xunit;

namespace KinkLinkCommonTests.Serialization;

public class NetworkCommandsSerializationTests
{
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData)
            .WithResolver(ContractlessStandardResolver.Instance);

    private static byte[] Serialize<T>(T obj) => MessagePackSerializer.Serialize(obj, MessagePackOptions);
    private static T Deserialize<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data, MessagePackOptions)!;

    private static DateTime TrimPrecision(DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);

    private static UserPermissions CreateFullPermissions() => new(
        uid: "test-uid-123",
        expires: DateTime.UtcNow.AddDays(7),
        priority: RelationshipPriority.Serious,
        controlsPerm: true,
        controlsConfig: true,
        disableSafeword: false,
        interactions: (int)(InteractionPerms.CanApplyGag | InteractionPerms.CanLockGag | InteractionPerms.CanApplyWardrobe)
    );

    private static UserPermissions CreateMinimalPermissions() => new(
        uid: "min-uid",
        expires: null,
        priority: RelationshipPriority.Casual,
        controlsPerm: false,
        controlsConfig: false,
        disableSafeword: false,
        interactions: 0
    );

    private static UserPermissions CreateExpiredPermissions() => new(
        uid: "expired-uid",
        expires: DateTime.UtcNow.AddDays(-1),
        priority: RelationshipPriority.Devotional,
        controlsPerm: true,
        controlsConfig: true,
        disableSafeword: true,
        interactions: int.MaxValue
    );

    private static byte[] CreateInvalidMessagePack() => new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };

    private static void AssertPermissionsEquivalent(UserPermissions expected, UserPermissions actual)
    {
        Assert.Equal(expected.PairUid, actual.PairUid);
        Assert.Equal(expected.Priority, actual.Priority);
        Assert.Equal(expected.ControlsPerm, actual.ControlsPerm);
        Assert.Equal(expected.ControlsConfig, actual.ControlsConfig);
        Assert.Equal(expected.DisableSafeword, actual.DisableSafeword);
        Assert.Equal(expected.Perms, actual.Perms);

        if (expected.Expires.HasValue && actual.Expires.HasValue)
        {
            Assert.Equal(TrimPrecision(expected.Expires.Value), TrimPrecision(actual.Expires.Value));
        }
        else
        {
            Assert.Equal(expected.Expires, actual.Expires);
        }
    }

    #region SyncPermissionsCommand Tests

    [Fact]
    public void SyncPermissionsCommand_RoundTrip_FullPermissions_PreservesAllData()
    {
        var original = new SyncPermissionsCommand(
            "TEST-FRIEND-CODE-123",
            CreateFullPermissions()
        );

        var data = Serialize(original);
        var deserialized = Deserialize<SyncPermissionsCommand>(data);

        Assert.Equal(original.SenderFriendCode, deserialized.SenderFriendCode);
        AssertPermissionsEquivalent(original.PermissionsGrantedBySender, deserialized.PermissionsGrantedBySender);
    }

    [Fact]
    public void SyncPermissionsCommand_RoundTrip_MinimalPermissions_PreservesData()
    {
        var original = new SyncPermissionsCommand(
            "MINIMAL-FC",
            CreateMinimalPermissions()
        );

        var data = Serialize(original);
        var deserialized = Deserialize<SyncPermissionsCommand>(data);

        Assert.Equal(original.SenderFriendCode, deserialized.SenderFriendCode);
        AssertPermissionsEquivalent(original.PermissionsGrantedBySender, deserialized.PermissionsGrantedBySender);
    }

    [Fact]
    public void SyncPermissionsCommand_RoundTrip_NullPermissions_HandlesCorrectly()
    {
        var original = new SyncPermissionsCommand(
            "NULL-PERMS-FC",
            new UserPermissions(true, RelationshipPriority.Casual, InteractionPerms.None)
        );

        var data = Serialize(original);
        var deserialized = Deserialize<SyncPermissionsCommand>(data);

        Assert.Equal(original.SenderFriendCode, deserialized.SenderFriendCode);
        Assert.Equal(original.PermissionsGrantedBySender.Perms, deserialized.PermissionsGrantedBySender.Perms);
    }

    [Fact]
    public void SyncPermissionsCommand_RoundTrip_ExpiredPermissions_PreservesData()
    {
        var original = new SyncPermissionsCommand(
            "EXPIRED-FC",
            CreateExpiredPermissions()
        );

        var data = Serialize(original);
        var deserialized = Deserialize<SyncPermissionsCommand>(data);

        Assert.Equal(original.SenderFriendCode, deserialized.SenderFriendCode);
        Assert.NotNull(deserialized.PermissionsGrantedBySender.Expires);
        Assert.True(deserialized.PermissionsGrantedBySender.Expires < DateTime.UtcNow);
    }

    [Fact]
    public void SyncPermissionsCommand_RoundTrip_AllInteractionFlags_PreservesData()
    {
        var perms = new UserPermissions(
            "all-flags-uid",
            DateTime.UtcNow.AddDays(1),
            RelationshipPriority.Devotional,
            true,
            true,
            false,
            (int)InteractionPerms.CanApplyGag | (int)InteractionPerms.CanLockGag | (int)InteractionPerms.CanUnlockGag |
            (int)InteractionPerms.CanRemoveGag | (int)InteractionPerms.CanApplyWardrobe | (int)InteractionPerms.CanLockMoodles
        );

        var original = new SyncPermissionsCommand("ALL-FLAGS-FC", perms);
        var data = Serialize(original);
        var deserialized = Deserialize<SyncPermissionsCommand>(data);

        Assert.Equal(original.PermissionsGrantedBySender.Perms, deserialized.PermissionsGrantedBySender.Perms);
    }

    [Fact]
    public void SyncPermissionsCommand_Invalid_MessagePack_ThrowsException()
    {
        var invalidData = CreateInvalidMessagePack();
        Assert.Throws<MessagePackSerializationException>(() => Deserialize<SyncPermissionsCommand>(invalidData));
    }

    [Fact]
    public void SyncPermissionsCommand_Empty_FriendCode_RoundTripsCorrectly()
    {
        var original = new SyncPermissionsCommand("", CreateMinimalPermissions());
        var data = Serialize(original);
        var deserialized = Deserialize<SyncPermissionsCommand>(data);

        Assert.Equal("", deserialized.SenderFriendCode);
    }

    #endregion

    #region SyncOnlineStatusCommand Tests

    [Theory]
    [InlineData(FriendOnlineStatus.Online)]
    [InlineData(FriendOnlineStatus.Offline)]
    [InlineData(FriendOnlineStatus.Pending)]
    public void SyncOnlineStatusCommand_RoundTrip_Status_PreservesValue(FriendOnlineStatus status)
    {
        var original = new SyncOnlineStatusCommand("STATUS-TEST-FC", status, CreateMinimalPermissions());
        var data = Serialize(original);
        var deserialized = Deserialize<SyncOnlineStatusCommand>(data);

        Assert.Equal(status, deserialized.Status);
    }

    [Fact]
    public void SyncOnlineStatusCommand_RoundTrip_WithFullPermissions_PreservesAllData()
    {
        var original = new SyncOnlineStatusCommand(
            "FULL-PERMS-FC",
            FriendOnlineStatus.Online,
            CreateFullPermissions()
        );

        var data = Serialize(original);
        var deserialized = Deserialize<SyncOnlineStatusCommand>(data);

        Assert.Equal(FriendOnlineStatus.Online, deserialized.Status);
        AssertPermissionsEquivalent(original.Permissions, deserialized.Permissions);
    }

    [Fact]
    public void SyncOnlineStatusCommand_RoundTrip_CombinedStatusAndPermissions_PreservesData()
    {
        var perms = new UserPermissions(
            "combined-uid",
            DateTime.UtcNow.AddDays(30),
            RelationshipPriority.Serious,
            true,
            false,
            true,
            (int)InteractionPerms.CanApplyGag
        );

        var original = new SyncOnlineStatusCommand("COMBINED-FC", FriendOnlineStatus.Pending, perms);
        var data = Serialize(original);
        var deserialized = Deserialize<SyncOnlineStatusCommand>(data);

        Assert.Equal(FriendOnlineStatus.Pending, deserialized.Status);
        Assert.Equal(RelationshipPriority.Serious, deserialized.Permissions.Priority);
    }

    [Fact]
    public void SyncOnlineStatusCommand_Invalid_MessagePack_ThrowsException()
    {
        var invalidData = CreateInvalidMessagePack();
        Assert.Throws<MessagePackSerializationException>(() => Deserialize<SyncOnlineStatusCommand>(invalidData));
    }

    [Fact]
    public void SyncOnlineStatusCommand_Empty_FriendCode_RoundTripsCorrectly()
    {
        var original = new SyncOnlineStatusCommand("", FriendOnlineStatus.Offline, CreateMinimalPermissions());
        var data = Serialize(original);
        var deserialized = Deserialize<SyncOnlineStatusCommand>(data);

        Assert.Equal("", deserialized.SenderFriendCode);
        Assert.Equal(FriendOnlineStatus.Offline, deserialized.Status);
    }

    #endregion

    #region LockStateDto Tests

    [Fact]
    public void LockStateDto_RoundTrip_Locked_State_PreservesAllData()
    {
        var original = new LockStateDto(
            "lock-001",
            true,
            "OwnerAlias",
            RelationshipPriority.Serious,
            true,
            DateTime.UtcNow.AddHours(1),
            "secret123"
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal(original.LockId, deserialized.LockId);
        Assert.Equal(original.IsLocked, deserialized.IsLocked);
        Assert.Equal(original.LockedByAlias, deserialized.LockedByAlias);
        Assert.Equal(original.LockPriority, deserialized.LockPriority);
        Assert.Equal(original.CanSelfUnlock, deserialized.CanSelfUnlock);
        Assert.Equal(original.Password, deserialized.Password);
    }

    [Fact]
    public void LockStateDto_RoundTrip_Unlocked_State_PreservesData()
    {
        var original = new LockStateDto(
            "lock-unlocked-001",
            false,
            "",
            RelationshipPriority.Casual,
            false,
            DateTime.MinValue,
            null
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal(original.LockId, deserialized.LockId);
        Assert.Equal(original.IsLocked, deserialized.IsLocked);
        Assert.Equal(original.LockedByAlias, deserialized.LockedByAlias);
        Assert.Equal(original.LockPriority, deserialized.LockPriority);
        Assert.Null(deserialized.Password);
    }

    [Theory]
    [InlineData(RelationshipPriority.Casual)]
    [InlineData(RelationshipPriority.Serious)]
    [InlineData(RelationshipPriority.Devotional)]
    public void LockStateDto_RoundTrip_Priority_PreservesValue(RelationshipPriority priority)
    {
        var original = new LockStateDto(
            $"priority-test-{priority}",
            true,
            "TestUser",
            priority,
            true,
            DateTime.UtcNow.AddDays(1),
            null
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal(priority, deserialized.LockPriority);
    }

    [Fact]
    public void LockStateDto_RoundTrip_NullPassword_PreservesNull()
    {
        var original = new LockStateDto(
            "null-pwd-lock",
            true,
            "User",
            RelationshipPriority.Casual,
            false,
            DateTime.UtcNow.AddHours(2),
            null
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Null(deserialized.Password);
    }

    [Fact]
    public void LockStateDto_RoundTrip_WithPassword_PreservesPassword()
    {
        var original = new LockStateDto(
            "with-pwd-lock",
            true,
            "User",
            RelationshipPriority.Casual,
            false,
            DateTime.UtcNow.AddHours(2),
            "my-secret-password"
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal("my-secret-password", deserialized.Password);
    }

    [Fact]
    public void LockStateDto_RoundTrip_Expired_Date_PreservesExpiredState()
    {
        var expiredTime = DateTime.UtcNow.AddDays(-1);
        var original = new LockStateDto(
            "expired-lock",
            true,
            "User",
            RelationshipPriority.Serious,
            false,
            expiredTime,
            null
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal(TrimPrecision(original.Expires), TrimPrecision(deserialized.Expires));
        Assert.True(deserialized.Expires < DateTime.UtcNow);
    }

    [Fact]
    public void LockStateDto_RoundTrip_Future_Date_PreservesFutureState()
    {
        var futureTime = DateTime.UtcNow.AddYears(1);
        var original = new LockStateDto(
            "future-lock",
            true,
            "User",
            RelationshipPriority.Devotional,
            true,
            futureTime,
            "pwd"
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal(TrimPrecision(original.Expires), TrimPrecision(deserialized.Expires));
        Assert.True(deserialized.Expires > DateTime.UtcNow);
    }

    [Fact]
    public void LockStateDto_RoundTrip_CanSelfUnlock_False_PreservesValue()
    {
        var original = new LockStateDto(
            "no-self-unlock",
            true,
            "Owner",
            RelationshipPriority.Serious,
            false,
            DateTime.UtcNow.AddDays(1),
            null
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.False(deserialized.CanSelfUnlock);
    }

    [Fact]
    public void LockStateDto_RoundTrip_EmptyStrings_RoundTripsCorrectly()
    {
        var original = new LockStateDto(
            "",
            false,
            "",
            RelationshipPriority.Casual,
            false,
            DateTime.MinValue,
            null
        );

        var data = Serialize(original);
        var deserialized = Deserialize<LockStateDto>(data);

        Assert.Equal("", deserialized.LockId);
        Assert.Equal("", deserialized.LockedByAlias);
    }

    [Fact]
    public void LockStateDto_Invalid_MessagePack_ThrowsException()
    {
        var invalidData = CreateInvalidMessagePack();
        Assert.Throws<MessagePackSerializationException>(() => Deserialize<LockStateDto>(invalidData));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(255)]
    public void LockStateDto_Invalid_PriorityValue_HandlesGracefully(int invalidValue)
    {
        var bytes = MessagePackSerializer.Serialize(new Dictionary<string, object>
        {
            ["LockId"] = "test-lock",
            ["IsLocked"] = true,
            ["LockedByAlias"] = "alias",
            ["LockPriority"] = invalidValue,
            ["CanSelfUnlock"] = true,
            ["Expires"] = DateTime.UtcNow.AddDays(1),
            ["Password"] = (string?)null
        }, MessagePackOptions);

        var deserialized = Deserialize<LockStateDto>(bytes);
        Assert.NotNull(deserialized);
        Assert.Equal("test-lock", deserialized.LockId);
    }

    #endregion
}