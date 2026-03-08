using System.Text;
using System.Text.Json;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class WardrobeServiceTests : DatabaseServiceTestBase
{
    private readonly WardrobeDataService _wardrobeService;

    public WardrobeServiceTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        var config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<WardrobeDataService>();

        _wardrobeService = new WardrobeDataService(config, logger);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string CreateItemDataBase64(uint itemId)
    {
        var data = new { item = new { ItemId = itemId, Apply = true }, mods = new List<GlamourerMod>(), materials = new Dictionary<string, GlamourerMaterial>() };
        return JsonSerializer.Serialize(data);
    }

    private static string CreateSetDataBase64()
    {
        var designBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new GlamourerDesign(), JsonOptions)));
        var data = new { design = designBase64, item = (object?)null, mods = new List<GlamourerMod>(), materials = new Dictionary<string, GlamourerMaterial>() };
        return JsonSerializer.Serialize(data);
    }

    private static string CreateModItemDataBase64(List<GlamourerMod> mods)
    {
        var data = new { item = (object?)null, mods, materials = new Dictionary<string, GlamourerMaterial>() };
        return JsonSerializer.Serialize(data);
    }

    private static string CreateItemData(GlamourerItem item)
    {
        var data = new { item, mods = new List<GlamourerMod>(), materials = new Dictionary<string, GlamourerMaterial>() };
        return JsonSerializer.Serialize(data);
    }

    private static string CreateSetData(GlamourerDesign design)
    {
        var designBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(design, JsonOptions)));
        var data = new { design = designBase64, item = (object?)null, mods = new List<GlamourerMod>(), materials = new Dictionary<string, GlamourerMaterial>() };
        return JsonSerializer.Serialize(data);
    }

    private static string CreateModItemData(List<GlamourerMod> mods)
    {
        var data = new { item = (object?)null, mods, materials = new Dictionary<string, GlamourerMaterial>() };
        return JsonSerializer.Serialize(data);
    }

    #region GetAllWardrobeByTypeAsync Tests

    [Fact]
    public async Task GetAllWardrobeByType_ItemType_ReturnsItems()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111111, "WARDTEST1");

        var itemId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = itemId,
            ProfileId = profileId,
            Name = "Test Item",
            Type = "item",
            Priority = 1,
            Data = CreateItemData(new GlamourerItem { ItemId = 12345, Apply = true })
        });

        var result = await _wardrobeService.GetAllWardrobeByTypeAsync(profileId, "item");

        Assert.Single(result);
        Assert.Equal("Test Item", result[0].Name);
    }

    [Fact]
    public async Task GetAllWardrobeByType_SetType_ReturnsSets()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111112, "WARDTEST2");

        var setId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = setId,
            ProfileId = profileId,
            Name = "Test Set",
            Type = "set",
            Priority = 1,
            Data = CreateSetData(new GlamourerDesign())
        });

        var result = await _wardrobeService.GetAllWardrobeByTypeAsync(profileId, "set");

        Assert.Single(result);
        Assert.Equal("Test Set", result[0].Name);
    }

    [Fact]
    public async Task GetAllWardrobeByType_ModItemType_ReturnsModItems()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111113, "WARDTEST3");

        var modItemId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = modItemId,
            ProfileId = profileId,
            Name = "Test ModItem",
            Type = "moditem",
            Priority = 1,
            Data = CreateModItemData([new GlamourerMod { Name = "TestMod", Enabled = true }])
        });

        var result = await _wardrobeService.GetAllWardrobeByTypeAsync(profileId, "moditem");

        Assert.Single(result);
        Assert.Equal("Test ModItem", result[0].Name);
    }

    [Fact]
    public async Task GetAllWardrobeByType_NoItems_ReturnsEmpty()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111114, "WARDTEST4");

        var result = await _wardrobeService.GetAllWardrobeByTypeAsync(profileId, "item");

        Assert.Empty(result);
    }

    #endregion

    #region GetWardrobeItemByGuid Tests

    [Fact]
    public async Task GetWardrobeItemByGuid_Exists_ReturnsItem()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111115, "WARDTEST5");

        var itemId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = itemId,
            ProfileId = profileId,
            Name = "Get Test Item",
            Type = "item",
            Priority = 1,
            Data = CreateItemData(new GlamourerItem { ItemId = 54321, Apply = true })
        });

        var result = await _wardrobeService.GetWardrobeItemByGuid(profileId, itemId);

        Assert.NotNull(result);
        Assert.Equal("Get Test Item", result.Name);
    }

    [Fact]
    public async Task GetWardrobeItemByGuid_NotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111116, "WARDTEST6");

        var result = await _wardrobeService.GetWardrobeItemByGuid(profileId, Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region CreateOrUpdateWardrobeItemsByNameAsync Tests

    [Fact]
    public async Task CreateOrUpdate_NewItem_CreatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111117, "WARDTEST7");

        var itemId = Guid.NewGuid();
        var dto = new WardrobeDto(
            itemId,
            "Created Item",
            "Test description",
            "item",
            GlamourerEquipmentSlot.Head,
            CreateItemDataBase64(11111),
            RelationshipPriority.Casual
        );

        var result = await _wardrobeService.CreateOrUpdateWardrobeItemsByNameAsync(profileId, itemId, dto);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeItemByGuid(profileId, itemId);
        Assert.NotNull(saved);
        Assert.Equal("Created Item", saved.Name);
        Assert.Equal("Test description", saved.Description);
    }

    [Fact]
    public async Task CreateOrUpdate_ExistingItem_UpdatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111118, "WARDTEST8");

        var itemId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = itemId,
            ProfileId = profileId,
            Name = "Original Name",
            Type = "item",
            Priority = 0,
            Data = CreateItemData(new GlamourerItem { ItemId = 11111, Apply = true })
        });

        var dto = new WardrobeDto(
            itemId,
            "Updated Name",
            "Updated description",
            "item",
            GlamourerEquipmentSlot.Body,
            CreateItemDataBase64(22222),
            RelationshipPriority.Devotional
        );

        var result = await _wardrobeService.CreateOrUpdateWardrobeItemsByNameAsync(profileId, itemId, dto);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeItemByGuid(profileId, itemId);
        Assert.NotNull(saved);
        Assert.Equal("Updated Name", saved.Name);
    }

    [Fact]
    public async Task CreateOrUpdate_NewSet_CreatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111119, "WARDTEST9");

        var setId = Guid.NewGuid();
        var dto = new WardrobeDto(
            setId,
            "Created Set",
            string.Empty,
            "set",
            GlamourerEquipmentSlot.None,
            CreateSetDataBase64(),
            RelationshipPriority.Casual
        );

        var result = await _wardrobeService.CreateOrUpdateWardrobeItemsByNameAsync(profileId, setId, dto);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeItemByGuid(profileId, setId);
        Assert.NotNull(saved);
        Assert.Equal("Created Set", saved.Name);
    }

    [Fact]
    public async Task CreateOrUpdate_ExistingSet_UpdatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111120, "WARDTEST10");

        var setId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = setId,
            ProfileId = profileId,
            Name = "Original Set",
            Type = "set",
            Priority = 0,
            Data = CreateSetData(new GlamourerDesign())
        });

        var dto = new WardrobeDto(
            setId,
            "Updated Set",
            string.Empty,
            "set",
            GlamourerEquipmentSlot.None,
            CreateSetDataBase64(),
            RelationshipPriority.Devotional
        );

        var result = await _wardrobeService.CreateOrUpdateWardrobeItemsByNameAsync(profileId, setId, dto);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeItemByGuid(profileId, setId);
        Assert.NotNull(saved);
        Assert.Equal("Updated Set", saved.Name);
    }

    [Fact]
    public async Task CreateOrUpdate_NewModItem_CreatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111121, "WARDTEST11");

        var modItemId = Guid.NewGuid();
        var dto = new WardrobeDto(
            modItemId,
            "Created ModItem",
            string.Empty,
            "moditem",
            GlamourerEquipmentSlot.None,
            CreateModItemDataBase64([new GlamourerMod { Name = "TestMod", Enabled = true }]),
            RelationshipPriority.Casual
        );

        var result = await _wardrobeService.CreateOrUpdateWardrobeItemsByNameAsync(profileId, modItemId, dto);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeItemByGuid(profileId, modItemId);
        Assert.NotNull(saved);
        Assert.Equal("Created ModItem", saved.Name);
    }

    [Fact]
    public async Task CreateOrUpdate_ExistingModItem_UpdatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111122, "WARDTEST12");

        var modItemId = Guid.NewGuid();
        await TestHarness.InsertTestWardrobeAsync(new InsertTestWardrobeParams
        {
            Id = modItemId,
            ProfileId = profileId,
            Name = "Original ModItem",
            Type = "moditem",
            Priority = 0,
            Data = CreateModItemData([new GlamourerMod { Name = "OldMod", Enabled = false }])
        });

        var dto = new WardrobeDto(
            modItemId,
            "Updated ModItem",
            string.Empty,
            "moditem",
            GlamourerEquipmentSlot.None,
            CreateModItemDataBase64([new GlamourerMod { Name = "NewMod", Enabled = true }]),
            RelationshipPriority.Devotional
        );

        var result = await _wardrobeService.CreateOrUpdateWardrobeItemsByNameAsync(profileId, modItemId, dto);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeItemByGuid(profileId, modItemId);
        Assert.NotNull(saved);
        Assert.Equal("Updated ModItem", saved.Name);
    }

    #endregion

    #region UpdateWardrobeStateAsync Tests

    [Fact]
    public async Task UpdateWardrobeState_NewState_CreatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111123, "WARDTEST13");

        var state = new WardrobeStateDto(
            null,
            new Dictionary<string, WardrobeItemData>
            {
                ["Head"] = new WardrobeItemData(Guid.NewGuid(), "Test", "Desc", GlamourerEquipmentSlot.Head, null, null, null, RelationshipPriority.Casual)
            },
            null
        );

        var result = await _wardrobeService.UpdateWardrobeStateAsync(profileId, state);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeStateAsync(profileId);
        Assert.NotNull(saved);
        Assert.Null(saved.BaseLayerBase64);
        Assert.NotNull(saved.Equipment);
        Assert.True(saved.Equipment.ContainsKey("Head"));
    }

    [Fact]
    public async Task UpdateWardrobeState_ExistingState_UpdatesSuccessfully()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111124, "WARDTEST14");

        var headId = Guid.NewGuid();
        var initialState = new WardrobeStateDto(
            null,
            new Dictionary<string, WardrobeItemData>
            {
                ["Head"] = new WardrobeItemData(headId, "Test", "Desc", GlamourerEquipmentSlot.Head, null, null, null, RelationshipPriority.Casual)
            },
            null
        );

        await _wardrobeService.UpdateWardrobeStateAsync(profileId, initialState);

        var updatedHeadId = Guid.NewGuid();
        var updatedBodyId = Guid.NewGuid();
        var updatedState = new WardrobeStateDto(
            null,
            new Dictionary<string, WardrobeItemData>
            {
                ["Head"] = new WardrobeItemData(updatedHeadId, "Test", "Desc", GlamourerEquipmentSlot.Head, null, null, null, RelationshipPriority.Casual),
                ["Body"] = new WardrobeItemData(updatedBodyId, "Test", "Desc", GlamourerEquipmentSlot.Body, null, null, null, RelationshipPriority.Casual)
            },
            null
        );

        var result = await _wardrobeService.UpdateWardrobeStateAsync(profileId, updatedState);

        Assert.True(result);

        var saved = await _wardrobeService.GetWardrobeStateAsync(profileId);
        Assert.NotNull(saved);
        Assert.Null(saved.BaseLayerBase64);
        Assert.NotNull(saved.Equipment);
        Assert.True(saved.Equipment.ContainsKey("Head"));
        Assert.True(saved.Equipment.ContainsKey("Body"));
    }

    #endregion

    #region GetWardrobeStateAsync Tests

    [Fact]
    public async Task GetWardrobeState_Exists_ReturnsState()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111125, "WARDTEST15");
        var wardrobeItemId = Guid.NewGuid();

        var designJson = JsonSerializer.Serialize(new GlamourerDesign(), JsonOptions);
        var designBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(designJson));

        await TestHarness.InsertTestActiveWardrobeAsync(new InsertTestActiveWardrobeParams
        {
            ProfileId = profileId,
            Glamourerset = designBase64,
            Head = JsonSerializer.SerializeToElement(new WardrobeItemData(wardrobeItemId, "Test Head", "", GlamourerEquipmentSlot.Head, new GlamourerItem { ItemId = 3000, Apply = true }, null, null, RelationshipPriority.Casual))
        });

        var result = await _wardrobeService.GetWardrobeStateAsync(profileId);

        Assert.NotNull(result);
        Assert.NotNull(result.BaseLayerBase64);
        Assert.NotNull(result.Equipment);
        Assert.True(result.Equipment.Count > 0);
    }

    [Fact]
    public async Task GetWardrobeState_NotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (profileId, _, _) = await CreateTestUserWithProfileAsync(111111111111111126, "WARDTEST16");

        var result = await _wardrobeService.GetWardrobeStateAsync(profileId);

        Assert.Null(result);
    }

    #endregion
}
