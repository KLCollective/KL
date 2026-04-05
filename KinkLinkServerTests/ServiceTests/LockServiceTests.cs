using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class LockServiceTests : DatabaseServiceTestBase
{
    private readonly LockService _lockService;

    public LockServiceTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        var config = new Configuration(
            fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var lockLogger = loggerFactory.CreateLogger<LockService>();
        _lockService = new LockService(config, lockLogger);
    }

    [Fact]
    public async Task GetAllLocksForUserAsync_ProfileNotFound_ReturnsEmptyList()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _lockService.GetAllLocksForUserAsync("NONEXISTENT");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllLocksForUserAsync_ProfileHasNoLocks_ReturnsEmptyList()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "NOLOCK1");

        var result = await _lockService.GetAllLocksForUserAsync(uid);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllLocksForUserAsync_ProfileHasLocks_ReturnsAllLocks()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "LOCKEE1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER1");

        var lock1 = new LockInfoDto
        {
            LockID = "lock_id_1",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };
        var lock2 = new LockInfoDto
        {
            LockID = "lock_id_2",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Serious,
            CanSelfUnlock = true,
            Expires = DateTime.UtcNow.AddDays(7),
            Password = "secret",
        };
        await _lockService.AddOrUpdateLockAsync(lock1);
        await _lockService.AddOrUpdateLockAsync(lock2);

        var result = await _lockService.GetAllLocksForUserAsync(lockeeUid);

        Assert.Equal(2, result.Count);
        var lockResult1 = result.First(r => r.LockID == "lock_id_1");
        Assert.Equal(RelationshipPriority.Casual, lockResult1.LockPriority);
        Assert.False(lockResult1.CanSelfUnlock);
        Assert.Null(lockResult1.Password);
        var lockResult2 = result.First(r => r.LockID == "lock_id_2");
        Assert.Equal(RelationshipPriority.Serious, lockResult2.LockPriority);
        Assert.True(lockResult2.CanSelfUnlock);
        Assert.Equal("secret", lockResult2.Password);
    }

    [Fact]
    public async Task GetLockAsync_ProfileNotFound_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _lockService.GetLockAsync("any_lock", "NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLockAsync_LockNotFound_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "NOLOCK2");

        var result = await _lockService.GetLockAsync("nonexistent_lock", uid);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLockAsync_LockExists_ReturnsLockInfoDto()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "GETLOCK1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER2");

        var lockInfo = new LockInfoDto
        {
            LockID = "get_lock_test",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Devotional,
            CanSelfUnlock = true,
            Expires = DateTime.UtcNow.AddDays(30),
            Password = "testpass",
        };
        await _lockService.AddOrUpdateLockAsync(lockInfo);

        var result = await _lockService.GetLockAsync("get_lock_test", lockeeUid);

        Assert.NotNull(result);
        var r = result.Value;
        Assert.Equal("get_lock_test", r.LockID);
        Assert.Equal(lockeeProfileId, r.LockeeID);
        Assert.Equal(lockerProfileId, r.LockerID);
        Assert.Equal(RelationshipPriority.Devotional, r.LockPriority);
        Assert.True(r.CanSelfUnlock);
        Assert.NotNull(r.Expires);
        Assert.Equal("testpass", r.Password);
    }

    [Fact]
    public async Task AddOrUpdateLockAsync_NewLock_ReturnsLockInfoDto()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, _) = await CreateTestUserWithProfileAsync(111111111111111111, "ADDLOCK1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER3");

        var lockInfo = new LockInfoDto
        {
            LockID = "new_lock_id",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };

        var result = await _lockService.AddOrUpdateLockAsync(lockInfo);

        Assert.NotNull(result);
        var r = result.Value;
        Assert.Equal("new_lock_id", r.LockID);
        Assert.Equal(lockeeProfileId, r.LockeeID);
        Assert.Equal(lockerProfileId, r.LockerID);
        Assert.Equal(RelationshipPriority.Casual, r.LockPriority);
        Assert.False(r.CanSelfUnlock);
        Assert.Null(r.Expires);
        Assert.Null(r.Password);
    }

    [Fact]
    public async Task AddOrUpdateLockAsync_ExistingLock_UpdatesAndReturns()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, _) = await CreateTestUserWithProfileAsync(111111111111111111, "UPDLOCK1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER4");

        var original = new LockInfoDto
        {
            LockID = "update_lock_test",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(original);

        var updated = new LockInfoDto
        {
            LockID = "update_lock_test",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Serious,
            CanSelfUnlock = true,
            Expires = DateTime.UtcNow.AddDays(14),
            Password = "newpass",
        };
        var result = await _lockService.AddOrUpdateLockAsync(updated);

        Assert.NotNull(result);
        var r = result.Value;
        Assert.Equal(RelationshipPriority.Serious, r.LockPriority);
        Assert.True(r.CanSelfUnlock);
        Assert.NotNull(r.Expires);
        Assert.Equal("newpass", r.Password);
    }

    [Fact]
    public async Task RemoveLockAsync_ProfileNotFound_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _lockService.RemoveLockAsync("any_lock", "NONEXISTENT");

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveLockAsync_LockNotFound_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "RMLOCK1");

        var result = await _lockService.RemoveLockAsync("nonexistent_lock", uid);

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveLockAsync_LockExists_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "RMLOCK2");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER5");

        var lockInfo = new LockInfoDto
        {
            LockID = "remove_lock_test",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(lockInfo);

        var result = await _lockService.RemoveLockAsync("remove_lock_test", lockeeUid);

        Assert.True(result);
        var remaining = await _lockService.GetLockAsync("remove_lock_test", lockeeUid);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task RemoveAllLocksForUserAsync_ProfileNotFound_ReturnsZero()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _lockService.RemoveAllLocksForUserAsync("NONEXISTENT");

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task RemoveAllLocksForUserAsync_UserHasLocks_ReturnsCount()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "RMALL1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER6");

        var lock1 = new LockInfoDto
        {
            LockID = "remove_all_1",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };
        var lock2 = new LockInfoDto
        {
            LockID = "remove_all_2",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Serious,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };
        var lock3 = new LockInfoDto
        {
            LockID = "remove_all_3",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Devotional,
            CanSelfUnlock = false,
            Expires = null,
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(lock1);
        await _lockService.AddOrUpdateLockAsync(lock2);
        await _lockService.AddOrUpdateLockAsync(lock3);

        var result = await _lockService.RemoveAllLocksForUserAsync(lockeeUid);

        Assert.Equal(3, result);
        var remaining = await _lockService.GetAllLocksForUserAsync(lockeeUid);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task PurgeExpiredLocksAsync_NoExpiredLocks_ReturnsZero()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "PURGE1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER7");

        var lockInfo = new LockInfoDto
        {
            LockID = "future_lock",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = DateTime.UtcNow.AddDays(30),
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(lockInfo);

        var result = await _lockService.PurgeExpiredLocksAsync();

        Assert.Equal(0, result);
        var remaining = await _lockService.GetLockAsync("future_lock", lockeeUid);
        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task PurgeExpiredLocksAsync_HasExpiredLocks_ReturnsCount()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "PURGE2");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER8");

        var expiredLock = new LockInfoDto
        {
            LockID = "expired_lock",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = DateTime.UtcNow.AddDays(-1),
            Password = null,
        };
        var validLock = new LockInfoDto
        {
            LockID = "valid_lock",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Serious,
            CanSelfUnlock = false,
            Expires = DateTime.UtcNow.AddDays(30),
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(expiredLock);
        await _lockService.AddOrUpdateLockAsync(validLock);

        var result = await _lockService.PurgeExpiredLocksAsync();

        Assert.Equal(1, result);
        var remaining = await _lockService.GetLockAsync("expired_lock", lockeeUid);
        Assert.Null(remaining);
        var stillValid = await _lockService.GetLockAsync("valid_lock", lockeeUid);
        Assert.NotNull(stillValid);
    }

    [Fact]
    public async Task HasExpiredLocksAsync_NoExpiredLocks_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, lockeeUid) = await CreateTestUserWithProfileAsync(111111111111111111, "HASEXP1");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKER9");

        var lockInfo = new LockInfoDto
        {
            LockID = "future_lock_check",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = DateTime.UtcNow.AddDays(10),
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(lockInfo);

        var result = await _lockService.HasExpiredLocksAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task HasExpiredLocksAsync_HasExpiredLocks_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, lockeeProfileId, _) = await CreateTestUserWithProfileAsync(111111111111111111, "HASEXP2");
        var (_, lockerProfileId, _) = await CreateTestUserWithProfileAsync(222222222222222222, "LOCKERA");

        var lockInfo = new LockInfoDto
        {
            LockID = "expired_lock_check",
            LockeeID = lockeeProfileId,
            LockerID = lockerProfileId,
            LockPriority = RelationshipPriority.Casual,
            CanSelfUnlock = false,
            Expires = DateTime.UtcNow.AddDays(-5),
            Password = null,
        };
        await _lockService.AddOrUpdateLockAsync(lockInfo);

        var result = await _lockService.HasExpiredLocksAsync();

        Assert.True(result);
    }
}
