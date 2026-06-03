using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class LocksHandlerTests : DatabaseServiceTestBase
{
    private readonly LocksHandler _locksHandler;
    private readonly LockService _lockService;
    private readonly Configuration _config;

    public LocksHandlerTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        _config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var lockLogger = loggerFactory.CreateLogger<LockService>();
        var handlerLogger = loggerFactory.CreateLogger<LocksHandler>();
        _lockService = new LockService(_config, lockLogger);
        _locksHandler = new LocksHandler(_lockService, PermissionsService, _config, handlerLogger);
    }

    #region GetAllLocksForUserAsync Tests

    [Fact]
    public async Task GetAllLocksForUserAsync_NoProfile_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _locksHandler.GetAllLocksForUserAsync("NONEXISTENT");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllLocksForUserAsync_NoLocks_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111050, "NOLOCKS1");

        var result = await _locksHandler.GetAllLocksForUserAsync(uid);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllLocksForUserAsync_HasLocks_ReturnsAll()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(
            111111111111111051, "LOCKEE10");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(
            222222222222222051, "LOCKER10");

        await _lockService.AddOrUpdateLockAsync(new LockInfoDto
        {
            LockID = "test-lock-1",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
        });

        var result = await _locksHandler.GetAllLocksForUserAsync(lockeeUid);

        Assert.Single(result);
        Assert.Equal("test-lock-1", result[0].LockID);
    }

    #endregion

    #region GetLocksForPairAsync Tests

    [Fact]
    public async Task GetLocksForPairAsync_NoProfile_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _locksHandler.GetLocksForPairAsync("NONEXISTENT", "ALSOFAKE");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLocksForPairAsync_NoLocks_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid1) = await CreateTestUserWithProfileAsync(111111111111111052, "PAIRL10");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(222222222222222052, "PAIRL20");

        var result = await _locksHandler.GetLocksForPairAsync(uid1, uid2);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLocksForPairAsync_HasLocks_ReturnsLocks()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(
            111111111111111053, "PAIRLOCK1");
        var (_, lockerProfileId, lockerUid) = await CreateTestUserWithProfileAsync(
            222222222222222053, "PAIRLOCK2");

        await _lockService.AddOrUpdateLockAsync(new LockInfoDto
        {
            LockID = "pair-lock-1",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Devotional,
            CanSelfUnlock = true,
            Password = "secret",
        });

        var result = await _locksHandler.GetLocksForPairAsync(lockeeUid, lockerUid);

        Assert.Single(result);
        Assert.Equal("pair-lock-1", result[0].LockID);
        Assert.Equal(RelationshipPriority.Devotional, result[0].LockPriority);
        Assert.True(result[0].CanSelfUnlock);
        Assert.Equal("secret", result[0].Password);
    }

    #endregion

    #region HandleAddLockAsync Tests (edge cases only)

    [Fact]
    public async Task HandleAddLockAsync_LockeeProfileNotFound_ReturnsTargetNotFriends()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, senderUid) = await CreateTestUserWithProfileAsync(
            111111111111111054, "ADDLOCK1");

        var lockInfo = new LockInfoDto
        {
            LockID = "wardrobe-hat",
            LockeeID = 99999,
            LockerID = 0,
            LockPriority = RelationshipPriority.Casual,
        };

        var result = await _locksHandler.HandleAddLockAsync(senderUid, lockInfo);

        Assert.Equal(ActionResultEc.TargetNotFriends, result.Result);
    }

    #endregion

    #region HandleRemoveLockAsync Tests (edge cases only)

    [Fact]
    public async Task HandleRemoveLockAsync_NoPermissions_ReturnsTargetNotFriends()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111062, "RMVLOCK1");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222062, "RMVLOCK2");

        var (result, _, _) = await _locksHandler.HandleRemoveLockAsync(
            uid1, "some-lock", uid2, null);

        Assert.Equal(ActionResultEc.TargetNotFriends, result.Result);
    }

    [Fact]
    public async Task HandleRemoveLockAsync_LockNotFound_ReturnsLockNotFound()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(
            111111111111111063, "RMVLOCK3");
        var (_, lockerProfileId, lockerUid) = await CreateTestUserWithProfileAsync(
            222222222222222063, "RMVLOCK4");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = lockerProfileId,
            PairId = lockeeProfileId,
            Priority = (int)RelationshipPriority.Serious,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = lockeeProfileId,
            PairId = lockerProfileId,
            Priority = (int)RelationshipPriority.Serious,
            Interaction = (long)InteractionPerms.CanLockWardrobe,
        });

        var (result, _, _) = await _locksHandler.HandleRemoveLockAsync(
            lockerUid, "nonexistent-lock", lockeeUid, null);

        Assert.Equal(ActionResultEc.LockNotFound, result.Result);
    }

    #endregion

    #region CheckCanModifySlotAsync Tests

    [Fact]
    public async Task CheckCanModifySlotAsync_NoExistingLock_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111067, "MODSLOT1");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222067, "MODSLOT2");

        var result = await _locksHandler.CheckCanModifySlotAsync(uid1, uid2, "wardrobe-hat");

        Assert.Equal(ActionResultEc.Success, result.Result);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CheckCanModifySlotAsync_LockedWithPermissions_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(
            111111111111111068, "MODSLOT3");
        var (_, lockerProfileId, lockerUid) = await CreateTestUserWithProfileAsync(
            222222222222222068, "MODSLOT4");

        // Create bidirectional pair with high priority
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = lockerProfileId,
            PairId = lockeeProfileId,
            Priority = (int)RelationshipPriority.Devotional,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = lockeeProfileId,
            PairId = lockerProfileId,
            Priority = (int)RelationshipPriority.Devotional,
            Interaction = (long)InteractionPerms.CanLockWardrobe,
        });

        // Low priority lock placed by someone else
        var (_, otherId, _) = await CreateTestUserWithProfileAsync(
            333333333333333068, "MODSLOT4B");
        await _lockService.AddOrUpdateLockAsync(new LockInfoDto
        {
            LockID = "wardrobe-hat",
            LockeeID = lockeeProfileId,
            LockerID = otherId,
            LockPriority = RelationshipPriority.Casual,
        });

        // High priority user can modify
        var result = await _locksHandler.CheckCanModifySlotAsync(lockerUid, lockeeUid, "wardrobe-hat");

        Assert.Equal(ActionResultEc.Success, result.Result);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CheckCanModifySlotAsync_LockedByOtherWithoutPermissions_ReturnsFailure()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(
            111111111111111069, "MODSLOT5");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(
            222222222222222069, "MODSLOT6");

        // Lock exists but no permissions pair
        await _lockService.AddOrUpdateLockAsync(new LockInfoDto
        {
            LockID = "wardrobe-hat",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Serious,
        });

        var result = await _locksHandler.CheckCanModifySlotAsync("UNAUTH1", lockeeUid, "wardrobe-hat");

        Assert.Equal(ActionResultEc.TargetNotFriends, result.Result);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task CheckCanModifySlotAsync_HigherPriority_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(
            111111111111111070, "MODSLOT8");
        var (_, lowLockerId, _) = await CreateTestUserWithProfileAsync(
            222222222222222070, "MODSLOT9");

        // Low priority lock
        await _lockService.AddOrUpdateLockAsync(new LockInfoDto
        {
            LockID = "wardrobe-hat",
            LockeeID = lockeeProfileId,
            LockerID = lowLockerId,
            LockPriority = RelationshipPriority.Casual,
        });

        // High priority pair
        var (_, highId, highUid) = await CreateTestUserWithProfileAsync(
            333333333333333070, "MODSLOT10");
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = highId,
            PairId = lockeeProfileId,
            Priority = (int)RelationshipPriority.Devotional,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = lockeeProfileId,
            PairId = highId,
            Priority = (int)RelationshipPriority.Devotional,
            Interaction = (long)InteractionPerms.CanLockWardrobe,
        });

        var result = await _locksHandler.CheckCanModifySlotAsync(
            highUid, lockeeUid, "wardrobe-hat");

        Assert.Equal(ActionResultEc.Success, result.Result);
        Assert.True(result.Value);
    }

    #endregion
}
