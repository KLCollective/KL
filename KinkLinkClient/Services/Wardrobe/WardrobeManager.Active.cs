using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.Glamourer.Domain;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public partial class WardrobeManager
{
    public async Task SyncFromServerAsync()
    {
        try
        {
            var result = await _wardrobeNetworkService.ListWardrobeItemsAsync();
            if (result.Result == ActionResultEc.Success && result.Value?.Items != null)
            {
                LoadFromWardrobeDto(result.Value.Items);
            }

            var statusResult = await _wardrobeNetworkService.GetWardrobeStatusAsync();
            if (statusResult.Result == ActionResultEc.Success && statusResult.Value?.State != null)
            {
                await ApplyWardrobeState(statusResult.Value.State);
            }

            NotificationHelper.Success("Wardrobe Sync", "Synced wardrobe from server");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Failed to sync from server");
            NotificationHelper.Error("Wardrobe Sync Failed", "Failed to sync wardrobe from server");
        }
    }

    public async Task ApplyWardrobeState(WardrobeStateDto state)
    {
        if (state.BaseLayerBase64 != null)
        {
            var baseLayerDesign = GlamourerDesignHelper.FromBase64(state.BaseLayerBase64);
            if (baseLayerDesign != null)
            {
                var baseLayerId = baseLayerDesign.Identifier;
                var set = GetSetById(baseLayerId);
                if (set != null)
                {
                    ApplySetByIdSync(baseLayerId);
                }
                else
                {
                    ActiveSet.SetBaseLayer(baseLayerDesign, RelationshipPriority.Casual);
                }
            }
        }

        if (state.Equipment != null)
        {
            foreach (var kvp in state.Equipment)
            {
                var itemData = kvp.Value;
                var slot = WardrobeSlotHelper.GetSlotFromName(kvp.Key);
                if (slot != GlamourerEquipmentSlot.None)
                {
                    var piece = new WardrobeItem
                    {
                        Id = itemData.Id,
                        Name = itemData.Name,
                        Description = itemData.Description,
                        Slot = itemData.Slot,
                        Item = itemData.Item,
                        Mods = itemData.Mods ?? [],
                        Materials = itemData.Materials ?? new Dictionary<string, GlamourerMaterial>(),
                        Priority = itemData.Priority,
                    };
                    ApplyPieceSync(slot, piece);
                }
            }
        }

        if (state.ModSettings != null)
        {
            foreach (var kvp in state.ModSettings)
            {
                var itemData = kvp.Value;
                var modItem = new WardrobeItem
                {
                    Id = itemData.Id,
                    Name = itemData.Name,
                    Description = itemData.Description,
                    Slot = itemData.Slot,
                    Item = itemData.Item,
                    Mods = itemData.Mods ?? [],
                    Materials = itemData.Materials ?? new Dictionary<string, GlamourerMaterial>(),
                    Priority = itemData.Priority,
                };
                ApplyCharacterItemSync(modItem);
            }
        }
        await SyncModItemsSafeAsync().ConfigureAwait(false);
    }

    private async Task SyncModItemsSafeAsync()
    {
        try
        {
            await SyncModItems();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] SyncModItems failed during ApplyWardrobeState");
        }
    }

    public void ApplyPieceSync(GlamourerEquipmentSlot slot, WardrobeItem piece)
    {
        ActiveSet.SetIndividual(slot, piece);
    }

    public void ApplyCharacterItemSync(WardrobeItem item)
    {
        ActiveSet.AddModItem(item);
    }

    public async Task ApplySetAsync(string name)
    {
        if (!_glamourerService.ApiAvailable)
        {
            Plugin.Log.Warning("Cannot apply set: Glamourer API not available");
            return;
        }

        var set = GetSetByName(name);
        if (set == null)
        {
            Plugin.Log.Warning("Cannot apply set: Set '{SetName}' not found in wardrobe", name);
            return;
        }

        var baseSetLockId = GetWardrobeLockId("baseset");
        if (_lockService.IsLocked(baseSetLockId))
        {
            Plugin.Log.Warning("Cannot apply set: BaseSet is locked");
            return;
        }

        Plugin.Log.Information(
            "Applying wardrobe set: {SetName} (ID: {SetId})",
            name,
            set.Design.Identifier
        );

        ActiveSet.SetBaseLayer(set.Design, set.Priority);

        await SyncModItems();

        await _glamourerService.ApplyDesignAsync(ActiveSet.GetCurrentState());

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully applied wardrobe set: {SetName}", name);
    }

    public async Task ApplyDesignFromPairAsync(
        GlamourerDesign design,
        RelationshipPriority priority
    )
    {
        if (!_glamourerService.ApiAvailable)
        {
            Plugin.Log.Warning("Cannot apply design: Glamourer API not available");
            return;
        }

        Plugin.Log.Information(
            "Applying wardrobe design from pair: {DesignName} (ID: {DesignId})",
            design.Name,
            design.Identifier
        );

        ActiveSet.SetBaseLayer(design, priority);

        await SyncModItems();

        await _glamourerService.ApplyDesignAsync(ActiveSet.GetCurrentState());

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information(
            "Successfully applied wardrobe design from pair: {DesignName}",
            design.Name
        );
    }

    public async Task RemoveActiveSetAsync()
    {
        if (!_glamourerService.ApiAvailable)
        {
            return;
        }

        var baseSetLockId = GetWardrobeLockId("baseset");
        var currentLock = _lockService.GetLock(baseSetLockId);
        if (currentLock != null && !currentLock.Value.CanSelfUnlock)
        {
            Plugin.Log.Warning("Cannot remove BaseSet: locked by another user");
            return;
        }

        Plugin.Log.Information("Removing active wardrobe set");

        await SyncModItems();
        ActiveSet.ClearBaseLayer();

        await _glamourerService.RevertToAutomation();

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully removed active wardrobe set");
    }

    public async Task ApplyPieceAsync(WardrobeItem piece)
    {
        var lockId = GetWardrobeLockId(piece.Slot);
        if (_lockService.IsLocked(lockId))
        {
            Plugin.Log.Warning("Cannot apply piece to slot {Slot}: slot is locked", piece.Slot);
            return;
        }

        Plugin.Log.Information(
            "Applying wardrobe piece: {PieceName} (ID: {PieceId}) to slot {Slot}",
            piece.Name,
            piece.Id,
            piece.Slot
        );

        ActiveSet.SetIndividual(piece.Slot, piece);

        await _glamourerService.ApplyDesignAsync(ActiveSet.GetCurrentState());

        await SyncModItems();
        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully applied wardrobe piece: {PieceName}", piece.Name);
    }

    public async Task ApplyWardrobeItem(WardrobeItem item)
    {
        ActiveSet.AddModItem(item);
        await SyncModItems();
        await SyncActiveSetToServerAsync();
    }

    public async Task ApplyCharacterItem(WardrobeItem item) => await ApplyWardrobeItem(item);

    public async Task RemoveWardrobeItemFromActive(Guid id)
    {
        ActiveSet.ClearModItem(id);
        await SyncModItems();
        await SyncActiveSetToServerAsync();
    }

    public async Task RemovePieceFromSlotAsync(GlamourerEquipmentSlot slot)
    {
        if (!_glamourerService.ApiAvailable || !ActiveSet.IsActive())
        {
            return;
        }

        var lockId = GetWardrobeLockId(slot);
        var currentLock = _lockService.GetLock(lockId);
        if (currentLock != null && !currentLock.Value.CanSelfUnlock)
        {
            Plugin.Log.Warning(
                "Cannot remove piece from slot {Slot}: slot is locked by another user",
                slot
            );
            return;
        }

        Plugin.Log.Information("Removing piece from slot: {Slot}", slot);

        ActiveSet.ClearIndividual(slot);
        await _glamourerService.RevertToAutomation();

        await SyncModItems();
        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully removed piece from slot: {Slot}", slot);
    }

    public async Task ClearActive()
    {
        ActiveSet.ClearBaseLayer();
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Head);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Body);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Hands);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Legs);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Feet);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Ears);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Neck);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Wrists);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.RFinger);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.LFinger);
        ActiveSet.ClearAllModItems();
        await _glamourerService.RevertToAutomation();
        _penumbraService.ClearAllTemporaryMods();
    }

    public List<SlotStatus> GetActiveSlotStatuses()
    {
        var statuses = new List<SlotStatus>();

        var baseLayer = ActiveSet.GetBaseLayer();
        statuses.Add(new SlotStatus("BaseSet", baseLayer != null, baseLayer?.Name, null));

        foreach (var slotName in WardrobeSlotHelper.AllSlotNames)
        {
            var slot = WardrobeSlotHelper.GetSlotFromName(slotName);
            var item = ActiveSet.GetIndividual(slot);
            var hasItem = item != null && item.Item != null && item.Item.ItemId != 0;
            var itemDisplay = hasItem ? $"Item {item!.Item!.ItemId}" : null;
            statuses.Add(new SlotStatus(slotName, hasItem, itemDisplay, null));
        }

        return statuses;
    }

    public async Task ReapplyIfChanged(GlamourerDesign design)
    {
        if (!ActiveSet.IsActive())
            return;

        var currentState = ActiveSet.GetCurrentState();
        if (!WardrobeSlotHelper.EquippedItemsChanged(design.Equipment, currentState.Equipment))
            return;

        Plugin.Log.Information("Detected equipment change, reapplying wardrobe");

        await _glamourerService.ApplyDesignAsync(currentState);
    }

    public async Task SyncModItems()
    {
        if (!_penumbraService.ApiAvailable)
            return;
        _penumbraService.ClearAllTemporaryMods();
        var modlist = ActiveSet.GetMods();
        foreach (var glamourerMod in modlist)
        {
            var mod = new Mod(glamourerMod.Name, glamourerMod.Directory);
            var settings = new ModSettings(
                glamourerMod.Settings,
                glamourerMod.Priority,
                glamourerMod.Enabled,
                glamourerMod.ForceInherit,
                glamourerMod.Remove
            );
            await _penumbraService.SetTemporaryModState(mod, settings, true);
        }
    }

    private async Task SyncActiveSetToServerAsync()
    {
        var baseLayerDesign = ActiveSet.GetBaseLayer();
        var baseLayerBase64 =
            baseLayerDesign != null ? GlamourerDesignHelper.ToBase64(baseLayerDesign) : null;
        var equipment = new Dictionary<string, WardrobeItemData>();
        var modSettings = new Dictionary<string, WardrobeItemData>();

        foreach (var slotName in WardrobeSlotHelper.AllSlotNames)
        {
            var slot = WardrobeSlotHelper.GetSlotFromName(slotName);
            var item = ActiveSet.GetIndividual(slot);
            if (item != null && item.Item != null && item.Item.ItemId != 0)
            {
                equipment[slotName] = new WardrobeItemData(
                    item.Id,
                    item.Name,
                    item.Description,
                    item.Slot,
                    item.Item,
                    item.Mods,
                    item.Materials,
                    item.Priority
                );
            }
        }

        foreach (var kvp in ActiveSet.GetCharacterItems())
        {
            var charItem = kvp.Value;
            if (charItem.Mods.Count > 0)
            {
                modSettings[charItem.Id.ToString()] = new WardrobeItemData(
                    charItem.Id,
                    charItem.Name,
                    charItem.Description,
                    charItem.Slot,
                    charItem.Item,
                    charItem.Mods,
                    charItem.Materials,
                    charItem.Priority
                );
            }
        }

        var state = new WardrobeStateDto(baseLayerBase64, equipment, modSettings);

        await _wardrobeNetworkService.SetWardrobeStatusAsync(new SetWardrobeStatusRequest(state));
    }
}
