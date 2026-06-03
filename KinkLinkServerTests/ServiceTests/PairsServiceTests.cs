using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using Npgsql;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class PairsServiceTests : DatabaseServiceTestBase
{
    private readonly PairsService _pairsService;

    public PairsServiceTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        var config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PairsService>();
        var metrics = new MetricsService();
        _pairsService = new PairsService(config, logger, metrics);
    }

    [Fact]
    public async Task GetAllPairsForProfileAsync_NoPairs_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111001, "PAIRTEST1");

        var result = await _pairsService.GetAllPairsForProfileAsync(uid);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllPairsForProfileAsync_HasPairs_ReturnsAll()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111002, "PAIRTEST2");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222002, "PAIRTEST3");

        // Create bidirectional pair (required for GetAllPairs to return)
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId2, PairId = profileId1 });

        var result = await _pairsService.GetAllPairsForProfileAsync(uid1);

        Assert.Single(result);
        Assert.Equal(profileId1, result[0].Item1);
        Assert.Equal(profileId2, result[0].Item2);
    }

    [Fact]
    public async Task GetAllPairsForProfileAsync_NonExistentUid_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.GetAllPairsForProfileAsync("NONEXISTENT");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProfileUidByIdAsync_ProfileExists_ReturnsUid()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(111111111111111003, "UIDTEST1");

        var result = await _pairsService.GetProfileUidByIdAsync(profileId);

        Assert.Equal(uid, result);
    }

    [Fact]
    public async Task GetProfileUidByIdAsync_ProfileNotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.GetProfileUidByIdAsync(99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPairByProfileIdsAsync_ByIds_PairExists_ReturnsPermissions()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111004, "GETPAIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222004, "GETPAIR2");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2,
            Priority = (int)RelationshipPriority.Devotional,
            ControlsPerm = true,
            ControlsConfig = true,
            DisableSafeword = false,
            Interaction = (long)(InteractionPerms.CanApplyGag | InteractionPerms.CanApplyWardrobe),
        });

        var result = await _pairsService.GetPairByProfileIdsAsync(profileId1, profileId2);

        Assert.NotNull(result);
        Assert.Equal(RelationshipPriority.Devotional, result.Priority);
        Assert.True(result.ControlsPerm);
        Assert.True(result.ControlsConfig);
        Assert.False(result.DisableSafeword);
        Assert.True(result.Perms.HasFlag(InteractionPerms.CanApplyGag));
        Assert.True(result.Perms.HasFlag(InteractionPerms.CanApplyWardrobe));
    }

    [Fact]
    public async Task GetPairByProfileIdsAsync_ByIds_PairNotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, _) = await CreateTestUserWithProfileAsync(111111111111111005, "GETPAIR3");

        var result = await _pairsService.GetPairByProfileIdsAsync(profileId1, 99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPairByProfileIdsAsync_ByUids_PairExists_ReturnsPermissions()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111006, "GETPAIR4");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222006, "GETPAIR5");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2,
            Priority = (int)RelationshipPriority.Serious,
        });

        var result = await _pairsService.GetPairByProfileIdsAsync(uid1, uid2);

        Assert.NotNull(result);
        Assert.Equal(RelationshipPriority.Serious, result.Priority);
    }

    [Fact]
    public async Task GetPairByProfileIdsAsync_ByUids_ProfileNotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111007, "GETPAIR6");

        var result = await _pairsService.GetPairByProfileIdsAsync(uid, "NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddPairAsync_NewPair_ReturnsPermissions()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111008, "ADDPAIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222008, "ADDPAIR2");

        var result = await _pairsService.AddPairAsync(profileId1, profileId2);

        Assert.NotNull(result);
        Assert.Equal(RelationshipPriority.Casual, result.Priority);
        Assert.False(result.ControlsPerm);
        Assert.False(result.ControlsConfig);
        Assert.False(result.DisableSafeword);
        Assert.Equal(0, (int)result.Perms);

        // Verify it was stored
        var fetched = await _pairsService.GetPairByProfileIdsAsync(profileId1, profileId2);
        Assert.NotNull(fetched);
    }

    [Fact]
    public async Task AddPairAsync_DuplicatePair_ThrowsException()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111009, "ADDPAIR3");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222009, "ADDPAIR4");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });

        // Duplicate key violates PK constraint - the generated SQL doesn't catch this
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            _pairsService.AddPairAsync(profileId1, profileId2));
    }

    [Fact]
    public async Task AddTemporaryPairAsync_WithExpiry_ReturnsPermissions()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111010, "TEMPPAIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222010, "TEMPPAIR2");

        // PostgreSQL timestamp without time zone can't accept DateTime with Kind=UTC
        var expires = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Unspecified);
        var result = await _pairsService.AddTemporaryPairAsync(profileId1, profileId2, expires);

        Assert.NotNull(result);
        Assert.NotNull(result.Expires);
        Assert.True(result.Expires.Value > DateTime.UtcNow);
    }

    [Fact]
    public async Task AddTemporaryPairAsync_WithoutExpiry_SetsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111011, "TEMPPAIR3");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222011, "TEMPPAIR4");

        var result = await _pairsService.AddTemporaryPairAsync(profileId1, profileId2, null);

        Assert.NotNull(result);
        Assert.Null(result.Expires);
    }

    [Fact]
    public async Task RemovePairAsync_ExistingPair_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111012, "RMVPAIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222012, "RMVPAIR2");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });

        var result = await _pairsService.RemovePairAsync(profileId1, profileId2);

        Assert.True(result);

        // Verify removed
        var fetched = await _pairsService.GetPairByProfileIdsAsync(profileId1, profileId2);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task RemovePairAsync_NonExistentPair_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.RemovePairAsync(1, 2);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdatePairPermissionsAsync_ExistingPair_UpdatesAndReturns()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111013, "UPDPAIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222013, "UPDPAIR2");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });

        var interactions = (int)(InteractionPerms.CanApplyGag | InteractionPerms.CanLockGag | InteractionPerms.CanApplyWardrobe);
        var result = await _pairsService.UpdatePairPermissionsAsync(profileId1, profileId2, interactions);

        Assert.NotNull(result);
        Assert.Equal(interactions, (int)result.Perms);

        // Verify persisted
        var fetched = await _pairsService.GetPairByProfileIdsAsync(profileId1, profileId2);
        Assert.NotNull(fetched);
        Assert.Equal(interactions, (int)fetched.Perms);
    }

    [Fact]
    public async Task UpdatePairPermissionsAsync_NonExistentPair_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.UpdatePairPermissionsAsync(1, 2, 42);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePairControlPermissionsAsync_ExistingPair_ReturnsNull()
    {
        // Method returns null on success per implementation
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111014, "CTRLPAIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222014, "CTRLPAIR2");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });

        var result = await _pairsService.UpdatePairControlPermissionsAsync(profileId1, profileId2, true, false, true);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePairControlPermissionsAsync_NonExistentPair_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.UpdatePairControlPermissionsAsync(1, 2, false, false, false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPairState_NoPair_ReturnsFalseFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, _) = await CreateTestUserWithProfileAsync(111111111111111015, "PAIRSTATE1");
        var (_, profileId2, _) = await CreateTestUserWithProfileAsync(222222222222222015, "PAIRSTATE2");

        var (atoB, btoA) = await _pairsService.GetPairState(profileId1, profileId2);

        Assert.False(atoB);
        Assert.False(btoA);
    }

    [Fact]
    public async Task GetPairState_OneSidedPair_ReturnsTrueFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111016, "PAIRSTATE3");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222016, "PAIRSTATE4");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });

        var (atoB, btoA) = await _pairsService.GetPairState(profileId1, profileId2);

        Assert.True(atoB);
        Assert.False(btoA);
    }

    [Fact]
    public async Task GetPairState_BidirectionalPair_ReturnsTrueTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111017, "PAIRSTATE5");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222017, "PAIRSTATE6");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId2, PairId = profileId1 });

        var (atoB, btoA) = await _pairsService.GetPairState(profileId1, profileId2);

        Assert.True(atoB);
        Assert.True(btoA);
    }

    [Fact]
    public async Task ConfirmTwoWayPairAsync_BidirectionalPair_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111018, "TWOWAY1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222018, "TWOWAY2");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId2, PairId = profileId1 });

        var result = await _pairsService.ConfirmTwoWayPairAsync(profileId1, profileId2);

        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmTwoWayPairAsync_OneSidedPair_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111019, "TWOWAY3");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222019, "TWOWAY4");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams { Id = profileId1, PairId = profileId2 });

        var result = await _pairsService.ConfirmTwoWayPairAsync(profileId1, profileId2);

        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmTwoWayPairAsync_NoPair_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.ConfirmTwoWayPairAsync(1, 2);

        Assert.False(result);
    }

    [Fact]
    public async Task PurgeExpiredPairsAsync_NoExpiredPairs_ReturnsZero()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.PurgeExpiredPairsAsync();

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task HasExpiredPairsAsync_NoExpiredPairs_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _pairsService.HasExpiredPairsAsync();

        Assert.False(result);
    }
}
