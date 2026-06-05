using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.Glamourer.Domain;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public partial class WardrobeManager
{
    public async Task SyncFromServerAsync()
    {
        var sw = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid();
        Plugin.Log.Information("[WardrobeManager] Enter SyncFromServerAsync correlationId={CorrelationId}", correlationId);
        try
        {
            var result = await _wardrobeNetworkService.ListWardrobeItemsAsync();
            if (result.Result == ActionResultEc.Success && result.Value != null)
            {
                WardrobeSyncHelper.ApplyServerItems(_wardrobeLibrary, result.Value.Items);
                Plugin.Log.Information("[WardrobeManager] SyncFromServerAsync correlationId={CorrelationId} syncedItems={Count}", correlationId, result.Value.Items.Count);
            }

            var statusResult = await _wardrobeNetworkService.GetWardrobeStatusAsync();
            if (statusResult.Result == ActionResultEc.Success && statusResult.Value?.State != null)
            {
                await HandleServerWardrobeStateAsync(statusResult.Value.State);
            }

            NotificationHelper.Success("Wardrobe Sync", "Synced wardrobe from server");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Failed to sync from server");
            NotificationHelper.Error("Wardrobe Sync Failed", "Failed to sync wardrobe from server");
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit SyncFromServerAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task ApplyWardrobeState(WardrobeStateDto state)
    {
        Plugin.Log.Information(
            $"[WardrobeManager] Enter ApplyWardrobeState layers={state?.Layers.Keys.ToList().ToString() ?? "None"} "
        );
        try
        {
            if (state != null)
            {
                ActiveSet.OverwriteWith(state, _wardrobeLibrary);
            }
            else
            {
                Plugin.Log.Warning("WardrobeStateDto is null");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Error in ApplyWardrobeState");
            throw;
        }
    }

    /// <summary>
    /// Handle a wardrobe state sync from the server and apply resulting design via Glamourer.
    /// Encapsulates Glamourer and Penumbra interactions within WardrobeManager to limit external deps.
    /// </summary>
    public async Task HandleServerWardrobeStateAsync(WardrobeStateDto state)
    {
        try
        {
            await ApplyWardrobeState(state).ConfigureAwait(false);

            // If there are active layers, sync mods then apply merged design via Glamourer
            if (ActiveSet.IsActive())
            {
                await SyncModItems();
                var merged = ActiveSet.GetCurrentState();
                await _glamourerService.ApplyDesignAsync(merged).ConfigureAwait(false);
            }
            else
            {
                // nothing active => revert automation
                await _glamourerService.RevertToAutomation().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Failed to handle server wardrobe state");
            throw;
        }
    }

    private async Task SyncModItemsSafeAsync()
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeManager] Enter SyncModItemsSafeAsync");
        try
        {
            await SyncModItems();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] SyncModItems failed during ApplyWardrobeState");
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit SyncModItemsSafeAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    // This for UI plumbing
    public async Task ApplyWardrobeLayerToActive(WardrobeLayer layer, Guid itemId)
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information(
            $"[WardrobeManager] Enter ApplyWardrobeLayerToActive layer={layer} itemId={itemId}"
        );
        try
        {
            var lockId = LockKindExtensions.From(layer);
            if (_lockService.IsLocked(lockId))
            {
                Plugin.Log.Warning(
                    "Cannot apply to slot {Slot}: slot is locked. Unlock first.",
                    layer
                );
                NotificationHelper.Warning(
                    "Slot Locked",
                    $"Cannot apply to {layer}: the slot is locked. Unlock it first."
                );
                return;
            }

            if (!_wardrobeLibrary.TryGetValue(itemId, out var item))
            {
                Plugin.Log.Warning("Wardrobe item not found locally: {Id}", itemId);
                return;
            }

            // Update local item's layer (library metadata) and notify server to change active state.
            item.Layer = layer;

            // Log info for debugging: layer, item id, item name, base64 size
            var base64 = GlamourerDesignHelper.ToBase64(item.Design) ?? string.Empty;
            Plugin.Log.Information(
                "[WardrobeManager] Applying layer -> layer={Layer} itemId={ItemId} name={Name} base64_len={Len}",
                layer,
                itemId,
                item.Name,
                base64.Length
            );

            // Do not change ActiveSet or apply locally. Active state must come from server only.
            await SyncActiveLayerToServer(layer, itemId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Error in ApplyWardrobeLayerToActive");
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit ApplyWardrobeLayerToActive duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    // This for UI plumbing
    public async Task RemoveWardrobeLayerFromActive(WardrobeLayer layer)
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information(
            $"[WardrobeManager] Enter RemoveWardrobeLayerFromActive layer={layer}"
        );
        try
        {
            // Remove any wardrobe items that belong to this layer locally.
            var toRemove = _wardrobeLibrary
                .Where(kvp => kvp.Value.Layer == layer)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var id in toRemove)
            {
                _wardrobeLibrary.Remove(id);
            }

            // Clear active layer on server.
            await _wardrobeNetworkService.ClearActiveWardrobeLayerAsync(layer);

            await SyncModItems();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Error in RemoveWardrobeLayerFromActive");
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit RemoveWardrobeLayerFromActive duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task RemovePieceFromSlotAsync(WardrobeLayer layer)
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information($"[WardrobeManager] Enter RemovePieceFromSlotAsync slot={layer}");
        try
        {
            if (!_glamourerService.ApiAvailable || !ActiveSet.IsActive())
            {
                return;
            }

            var lockId = GetWardrobeLockId(layer);
            var currentLock = _lockService.GetLock(lockId);
            if (currentLock != null && !currentLock.Value.CanSelfUnlock)
            {
                Plugin.Log.Warning(
                    "Cannot remove piece from slot {Slot}: slot is locked by another user.",
                    layer
                );
                NotificationHelper.Warning(
                    "Slot Locked",
                    $"Cannot remove from {layer}: the slot is locked by another user. Unlock it first."
                );
                return;
            }

            Plugin.Log.Information("Removing piece from slot: {Slot}", layer);

            ActiveSet.RemoveLayer(layer);
            await _glamourerService.RevertToAutomation();

            await SyncModItems();

            // Clear the removed layer on server and then sync remaining active layers.
            await _wardrobeNetworkService.ClearActiveWardrobeLayerAsync(layer);
            await SyncActiveSetToServerAsync();

            Plugin.Log.Information("Successfully removed piece from slot: {Slot}", layer);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Error in RemovePieceFromSlotAsync");
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit RemovePieceFromSlotAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task ClearActive()
    {
        Plugin.Log.Information("[WardrobeManager] Enter ClearActive");
        try
        {
            ActiveSet.Clear();
            await _glamourerService.RevertToAutomation();
            _penumbraService.ClearAllTemporaryMods();
            await SyncActiveSetToServerAsync();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Error in ClearActive");
            throw;
        }
    }

    public async Task ReapplyIfChanged(GlamourerDesign design)
    {
        if (!ActiveSet.IsActive())
            return;

        var currentState = ActiveSet.GetCurrentState();
        // Skip if equipment didn't change to avoid re-triggering Glamourer state events
        if (!WardrobeSlotHelper.EquippedItemsChanged(design.Equipment, currentState.Equipment))
            return;

        Plugin.Log.Information("Detected equipment change, reapplying wardrobe");

        try
        {
            var apiAvailable = _glamourerService.ApiAvailable;
            var base64 = GlamourerDesignHelper.ToBase64(currentState) ?? string.Empty;
            Plugin.Log.Information("[WardrobeManager] ReapplyIfChanged apiAvailable={ApiAvailable} base64_len={Len}", apiAvailable, base64.Length);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[WardrobeManager] Failed to compute base64 for current state");
        }

        await _glamourerService.ApplyDesignAsync(currentState);
    }

    public async Task SyncModItems()
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeManager] Enter SyncModItems");
        try
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
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Error in SyncModItems");
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit SyncModItems duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task RandomizeActiveAsync()
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeManager] Enter RandomizeActiveAsync");
        try
        {
            var response = await _wardrobeNetworkService.RandomizeActiveWardrobeAsync(
                new RandomizeActiveWardrobeRequest()
            );
            if (response.Result != ActionResultEc.Success)
            {
                NotificationHelper.Error(
                    "Randomize Wardrobe",
                    $"Failed to randomize wardrobe: {response.Result}"
                );
            }
            else
            {
                NotificationHelper.Success(
                    "Randomize Wardrobe",
                    "Requested randomization. Applying new outfit shortly."
                );
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Failed to request randomize active wardrobe");
            NotificationHelper.Error(
                "Randomize Wardrobe",
                "Failed to request randomize active wardrobe"
            );
            throw;
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeManager] Exit RandomizeActiveAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    private async Task SyncActiveLayerToServer(WardrobeLayer layer, Guid item)
    {
        await _wardrobeNetworkService.SetActiveWardrobeLayerAsync(layer, _wardrobeLibrary[item]);
    }

    private LockKind GetWardrobeLockId(WardrobeLayer layer)
    {
        return LockKindExtensions.From(layer);
    }

    private async Task SyncActiveSetToServerAsync()
    {
        try
        {
            // Iterate currently active layers rather than entire library.
            foreach (var kvp in ActiveSet.Layers)
            {
                var item = kvp.Value;
                await _wardrobeNetworkService.SetActiveWardrobeLayerAsync(item.Layer, item);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeManager] Failed to sync active set to server");
        }
    }
}
