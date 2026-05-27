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
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public partial class WardrobeManager
{
    public async Task SyncFromServerAsync()
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeManager] Enter SyncFromServerAsync");
        try
        {
            var result = await _wardrobeNetworkService.ListWardrobeItemsAsync();
            if (result.Result == ActionResultEc.Success && result.Value != null)
            {
                foreach (var item in result.Value.Items)
                {
                    this._wardrobeLibrary[item.Id] = new WardrobeItem
                    {
                        Design =
                            GlamourerDesignHelper.FromBase64(item.Base64GlamourerData)
                            ?? new GlamourerDesign(),
                        Priority = item.Priority,
                    };
                }
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
                ActiveSet.OverwriteWith(state);
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
            if (!_wardrobeLibrary.TryGetValue(itemId, out var item))
            {
                Plugin.Log.Warning("Wardrobe item not found locally: {Id}", itemId);
                return;
            }

            // Update local item's layer and ask server to apply it.
            item.Layer = layer;

            // Sync any mod changes and notify server of active layer change.
            await SyncModItems();
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

            // Clear active layer on server by sending an empty design for that layer.
            var emptyItem = new WardrobeItem { Design = new GlamourerDesign(), Layer = layer };
            await _wardrobeNetworkService.SetActiveWardrobeLayerAsync(layer, emptyItem);

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
                    "Cannot remove piece from slot {Slot}: slot is locked by another user",
                    layer
                );
                return;
            }

            Plugin.Log.Information("Removing piece from slot: {Slot}", layer);

            ActiveSet.RemoveLayer(layer);
            await _glamourerService.RevertToAutomation();

            await SyncModItems();
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
        // TODO: Optimization to skip if the designs are the same
        // if (!WardrobeSlotHelper.EquippedItemsChanged(design.Equipment, currentState.Equipment))
        //     return;

        Plugin.Log.Information("Detected equipment change, reapplying wardrobe");

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
                // Success - ActiveWardrobeWatcher will push the new state; notify the user
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

    private string GetWardrobeLockId(WardrobeLayer layer)
    {
        return $"wardrobe-{layer.ToString().ToLowerInvariant()}";
    }

    private async Task SyncActiveSetToServerAsync()
    {
        try
        {
            foreach (var kvp in _wardrobeLibrary)
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
