using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class RandomizeActiveWardrobeTests : DatabaseServiceTestBase
{
    private readonly ActiveWardrobeStateService _activeWardrobeService;

    public RandomizeActiveWardrobeTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        var config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var metricsService = new MetricsService();
        var lockLogger = loggerFactory.CreateLogger<LockService>();
        var lockService = new LockService(config, lockLogger);

        var sharedWardrobeSql = new WardrobeSql(config.DatabaseConnectionString);
        _activeWardrobeService = new ActiveWardrobeStateService(sharedWardrobeSql, loggerFactory.CreateLogger<ActiveWardrobeStateService>(), metricsService, lockService);
    }

    private static string CreateItemData(GlamourerItem item)
    {
        var data = new
        {
            item,
            mods = new List<GlamourerMod>(),
            materials = new Dictionary<string, GlamourerMaterial>(),
        };
        return JsonSerializer.Serialize(data);
    }

    [Fact]
    public async Task RandomizeActiveWardrobe_PersistsState()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(55555555555555555, "RANDTEST1");

        // Insert a few wardrobe items
        var headId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = headId,
            ProfileId = profileId,
            Name = "HeadItem",
            Type = "item",
            Slot = (int)GlamourerEquipmentSlot.Head,
            Priority = 1,
            Data = CreateItemData(new GlamourerItem { ItemId = 1001, Apply = true })
        });

        var bodyId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = bodyId,
            ProfileId = profileId,
            Name = "BodyItem",
            Type = "item",
            Slot = (int)GlamourerEquipmentSlot.Body,
            Priority = 1,
            Data = CreateItemData(new GlamourerItem { ItemId = 2001, Apply = true })
        });

        var result = await _activeWardrobeService.RandomizeActiveWardrobeAsync(profileId);

        Assert.True(result);

        var state = await _activeWardrobeService.GetWardrobeStateAsync(profileId);
        Assert.NotNull(state);
        Assert.NotNull(state.Layers);
        Assert.True(state.Layers.Count > 0);
    }
}
