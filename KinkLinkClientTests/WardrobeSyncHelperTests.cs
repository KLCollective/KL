using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Dependencies.Glamourer;
using Xunit;

namespace KinkLinkClientTests;

public class WardrobeSyncHelperTests
{
    [Fact]
    public void ApplyServerItems_RemovesMissingAndPreservesMeta()
    {
        var local = new Dictionary<Guid, WardrobeItem>();
        var keepId = Guid.NewGuid();
        var removeId = Guid.NewGuid();

        var existing = new WardrobeItem { Name = "Old", Description = "OldDesc", Layer = WardrobeLayer.Chest };
        existing.Id = keepId;
        local[keepId] = existing;

        var toRemove = new WardrobeItem { Name = "Old2" };
        toRemove.Id = removeId;
        local[removeId] = toRemove;

        // Use empty base64 so FromBase64 yields null and defaults to new GlamourerDesign
        var base64 = string.Empty;

        var serverItems = new List<WardrobeDto>
        {
            new WardrobeDto(keepId, "ServerName", "ServerDesc", WardrobeLayer.Chest, base64, RelationshipPriority.Casual)
        };

        WardrobeSyncHelper.ApplyServerItems(local, serverItems);

        Assert.Single(local);
        Assert.True(local.ContainsKey(keepId));
        var item = local[keepId];
        Assert.Equal("ServerName", item.Name);
        Assert.Equal("ServerDesc", item.Description);
        Assert.Equal(WardrobeLayer.Chest, item.Layer);
    }

    [Fact(Skip = "Integration test requiring plugin runtime")]
    public async Task RemoveWardrobeLayerFromActive_CallsClear()
    {
        // Prepare uninitialized WardrobeManager
        var mgrObj = FormatterServices.GetUninitializedObject(typeof(WardrobeManager));
        var mgr = (WardrobeManager)mgrObj;

        // Prepare local library with one item in Head layer
        var dict = new Dictionary<Guid, WardrobeItem>();
        var id = Guid.NewGuid();
        var item = new WardrobeItem { Layer = WardrobeLayer.Head };
        item.Id = id;
        dict[id] = item;

        // Create fake wardrobe network service
        var fakeNet = new FakeWardrobeNetworkService();

        // Set private fields
        typeof(WardrobeManager).GetField("_wardrobeLibrary", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mgr, dict);
        typeof(WardrobeManager).GetField("_wardrobeNetworkService", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mgr, fakeNet);

        // Set penumbra service to uninitialized instance so ApiAvailable default false (SyncModItems will return early)
        var penumbraObj = FormatterServices.GetUninitializedObject(typeof(KinkLinkClient.Dependencies.Penumbra.Services.PenumbraService));
        typeof(WardrobeManager).GetField("_penumbraService", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mgr, penumbraObj);

        // Set lock service instance
        var lockService = new KinkLinkClient.Services.LockService();
        typeof(WardrobeManager).GetField("_lockService", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mgr, lockService);

        // Invoke RemoveWardrobeLayerFromActive
        var method = typeof(WardrobeManager).GetMethod("RemoveWardrobeLayerFromActive", BindingFlags.Public | BindingFlags.Instance)!;
        var task = (Task)method.Invoke(mgr, new object[] { WardrobeLayer.Head })!;
        await task;

        Assert.Equal(WardrobeLayer.Head, fakeNet.ClearedLayer);
    }

    private class FakeWardrobeNetworkService : IWardrobeNetworkService
    {
        public WardrobeLayer ClearedLayer = default;

        public Task<ActionResult<ListWardrobeItemsResponse>> ListWardrobeItemsAsync()
        {
            return Task.FromResult(new ActionResult<ListWardrobeItemsResponse>(ActionResultEc.Success, null));
        }

        public Task<ActionResult<GetWardrobeStatusResponse>> GetWardrobeStatusAsync()
        {
            return Task.FromResult(new ActionResult<GetWardrobeStatusResponse>(ActionResultEc.ValueNotSet, null));
        }

        public Task<ActionResult<bool>> SetActiveWardrobeLayerAsync(WardrobeLayer layer, WardrobeItem item)
        {
            return Task.FromResult(new ActionResult<bool>(ActionResultEc.Success, true));
        }

        public Task<ActionResult<bool>> ClearActiveWardrobeLayerAsync(WardrobeLayer layer)
        {
            ClearedLayer = layer;
            return Task.FromResult(new ActionResult<bool>(ActionResultEc.Success, true));
        }

        public Task<ActionResult<RandomizeActiveWardrobeResponse>> RandomizeActiveWardrobeAsync(RandomizeActiveWardrobeRequest request)
        {
            return Task.FromResult(new ActionResult<RandomizeActiveWardrobeResponse>(ActionResultEc.Success, null));
        }

        public Task<ActionResult<AddWardrobeItemResponse>> AddWardrobeItemAsync(AddWardrobeItemRequest request)
        {
            return Task.FromResult(new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Success, null));
        }

        public Task<ActionResult<RemoveWardrobeItemResponse>> RemoveWardrobeItemAsync(RemoveWardrobeItemRequest request)
        {
            return Task.FromResult(new ActionResult<RemoveWardrobeItemResponse>(ActionResultEc.Success, null));
        }

        public Task<ActionResult<GetWardrobeItemResponse>> GetWardrobeItemAsync(GetWardrobeItemRequest request)
        {
            return Task.FromResult(new ActionResult<GetWardrobeItemResponse>(ActionResultEc.ValueNotSet, null));
        }

        public Task<List<LightWardrobeItemDto>> QueryPairWardrobe(string friendCode)
        {
            return Task.FromResult(new List<LightWardrobeItemDto>());
        }
    }
}
