using KinkLinkCommon.Database;
using KinkLinkServer.Domain;
using Serilog;

namespace KinkLinkServer.Services;

public class TestDataSeeder
{
    private const string EnableEnvVar = "ENABLE_TEST_DATA";

    public static bool ShouldSeed()
    {
        var envValue = Environment.GetEnvironmentVariable(EnableEnvVar);
        if (envValue is ("true" or "1"))
        {
            return true;
        }
        return false;
    }

    public static async Task<bool> SeedAsync(Configuration config)
    {
        var _logger = Log.ForContext<TestDataSeeder>();
        var envValue = Environment.GetEnvironmentVariable(EnableEnvVar);
        if (envValue is not ("true" or "1"))
        {
            _logger.Debug(
                "Test data seeding disabled (ENABLE_TEST_DATA={Value})",
                envValue ?? "null"
            );
            return false;
        }

        var testHarnessSql = new TestHarnessSql(config.DatabaseConnectionString);
        _logger.Information("Seeding test data...");

        // These are intentionally insecure to help with debugging locally.
        // base key is "testhash1"
        // base key is "testhash2"
        // base key is "testhash3"
        var user1Id = await SeedUserAsync(
            testHarnessSql,
            111111111111111111,
            "294d0c59d9de98b48872c5a7f1a35f165fb137056ed2f29e844f0f3b9a75ee61",
            true,
            false
        );
        var user2Id = await SeedUserAsync(
            testHarnessSql,
            222222222222222222,
            "f68732f771f97d7e84371f7048ecc4c748f5450b23c0968a3bb3f7e1402f6cb3",
            true,
            false
        );
        var user3Id = await SeedUserAsync(
            testHarnessSql,
            333333333333333333,
            "7db6c1f599fc1210a2df2b775b94123a45c83e02750f0e31cbbff05b2db28404",
            true,
            false
        );

        var user4Id = await SeedUserAsync(
            testHarnessSql,
            444444444444444444,
            "59ab28d4a7b908c098e38fd735bf122ba2b30b88e62aed77a75e36c14274f84e",
            true,
            false
        );

        if (
            user1Id is not { } u1
            || user2Id is not { } u2
            || user3Id is not { } u3
            || user4Id is not { } u4
        )
        {
            _logger.Error("Failed to seed users");
            return false;
        }

        _logger.Information("Seeded {Count} users", 3);

        var profile1Id = await SeedProfileAsync(
            testHarnessSql,
            u1,
            "TEST000001",
            "Kinkster-0001",
            "Main",
            "Kinkster",
            "This is a profile description for user 1"
        );
        var profile2Id = await SeedProfileAsync(
            testHarnessSql,
            u2,
            "TEST000002",
            "Dominant-0002",
            "Alt",
            "Dominant",
            "This is a profile description for user 2"
        );
        var profile3Id = await SeedProfileAsync(
            testHarnessSql,
            u3,
            "TEST000003",
            "Doll-0004",
            "SecondAlt",
            "Submissive",
            "This is a profile description for user 3"
        );

        var profile4Id = await SeedProfileAsync(
            testHarnessSql,
            u3,
            "TEST000010",
            "Submissive-0010",
            "SecondAlt",
            "Submissive",
            "This is a profile description for user 4"
        );

        if (
            profile1Id is not { } p1
            || profile2Id is not { } p2
            || profile3Id is not { } p3
            || profile4Id is not { } p4
        )
        {
            _logger.Error("Failed to seed profiles");
            return false;
        }

        _logger.Information("Seeded {Count} profiles", 3);

        await SeedProfileConfigAsync(testHarnessSql, p1, true, true, true, true);
        await SeedProfileConfigAsync(testHarnessSql, p2, true, false, false, true);
        await SeedProfileConfigAsync(testHarnessSql, p3, false, true, true, false);

        _logger.Information("Seeded {Count} profile configs", 3);

        await SeedPairAsync(testHarnessSql, p2, p1, 1, true, true, false, 1 + 1 + 1);
        await SeedPairAsync(testHarnessSql, p3, p1, 0, false, false, false, 0);
        await SeedPairAsync(testHarnessSql, p1, p4, 0, false, false, false, 0);

        _logger.Information("Seeded {Count} pairs", 3);

        _logger.Information("Test data seeding complete");

        return true;
    }

    private static async Task<int?> SeedUserAsync(
        TestHarnessSql testHarnessSql,
        long discordId,
        string secretKeyHash,
        bool verified,
        bool banned
    )
    {
        var result = await testHarnessSql.SeedUserAsync(
            new(discordId, secretKeyHash, verified, banned)
        );
        return result?.Id;
    }

    private static async Task<int?> SeedProfileAsync(
        TestHarnessSql testHarnessSql,
        int userId,
        string uid,
        string chatRole,
        string alias,
        string title,
        string description
    )
    {
        var result = await testHarnessSql.SeedProfileAsync(
            new(userId, uid, chatRole, alias, title, description)
        );
        return result?.Id;
    }

    private static async Task SeedProfileConfigAsync(
        TestHarnessSql testHarnessSql,
        int profileId,
        bool enableGlamours,
        bool enableGarbler,
        bool enableGarblerChannels,
        bool enableMoodles
    )
    {
        await testHarnessSql.SeedProfileConfigAsync(
            new(profileId, enableGlamours, enableGarbler, enableGarblerChannels, enableMoodles)
        );
    }

    private static async Task SeedPairAsync(
        TestHarnessSql testHarnessSql,
        int id,
        int pairId,
        int priority,
        bool controlsPerm,
        bool controlsConfig,
        bool disableSafeword,
        int interactions
    )
    {
        await testHarnessSql.SeedPairAsync(
            new(
                id,
                pairId,
                priority,
                controlsPerm,
                controlsConfig,
                disableSafeword,
                (long)interactions
            )
        );
    }
}
