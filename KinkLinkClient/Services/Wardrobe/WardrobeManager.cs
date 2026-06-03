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
using KinkLinkCommon.Domain.Wardrobe;
using Newtonsoft.Json.Linq;

namespace KinkLinkClient.Services;

public partial class WardrobeManager : IDisposable
{
    private readonly LockService _lockService;
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly IWardrobeNetworkService _wardrobeNetworkService;

    private readonly Dictionary<Guid, WardrobeItem> _wardrobeLibrary =
        new Dictionary<Guid, WardrobeItem>();

    public ActiveWardrobe ActiveSet { get; }

    public IReadOnlyList<WardrobeItem> WardrobeLibrary => _wardrobeLibrary.Values.ToList();

    public WardrobeManager(
        LockService lockService,
        GlamourerService glamourerService,
        PenumbraService penumbraService,
        IWardrobeNetworkService wardrobeNetworkService
    )
    {
        _lockService = lockService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _wardrobeNetworkService = wardrobeNetworkService;
        ActiveSet = new ActiveWardrobe();

        _glamourerService.IpcReady += OnIpcReady;
        _penumbraService.IpcReady += OnPenumbraIpcReady;
        if (_glamourerService.ApiAvailable)
        {
            _ = RefreshGlamourerDesignsAsync();
        }
    }

    private void OnIpcReady(object? sender, EventArgs e)
    {
        Plugin.Log.Information(
            "Glamourer IPC became ready, refreshing designs and reapplying active wardrobe"
        );
        _ = RefreshGlamourerDesignsAsync();
        _ = ReapplyActiveWardrobeAsync();
    }

    private void OnPenumbraIpcReady(object? sender, EventArgs e)
    {
        Plugin.Log.Information(
            "Penumbra IPC became ready, reapplying active wardrobe mods"
        );
        _ = ReapplyActiveWardrobeAsync();
    }

    // Used for first sync
    private async Task ReapplyActiveWardrobeAsync()
    {
        try
        {
            if (!ActiveSet.IsActive())
                return;

            Plugin.Log.Information("[WardrobeManager] Reapplying active wardrobe after IPC ready");
            await SyncModItems().ConfigureAwait(false);
            var merged = ActiveSet.GetCurrentState();
            await _glamourerService.ApplyDesignAsync(merged).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "[WardrobeManager] Failed to reapply active wardrobe after IPC ready"
            );
        }
    }

    public bool IsLayerLocked(WardrobeLayer layer)
    {
        return _lockService.IsLocked(layer.ToString());
    }

    public bool GlamourerApiAvailable => _glamourerService.ApiAvailable;

    public async Task<List<Design>> RefreshGlamourerDesignsAsync()
    {
        if (
            _glamourerService.ApiAvailable
            && await _glamourerService.GetDesignList().ConfigureAwait(false) is { } designs
        )
        {
            return designs.OrderBy(d => d.Path).ToList();
        }
        else
        {
            return new List<Design>();
        }
    }

    public async Task<List<(Mod, ModSettings)>> GetAvailableModsAsync()
    {
        if (!_penumbraService.ApiAvailable)
        {
            return new();
        }
        return await _penumbraService.GetAllMods();
    }

    public void AddDesign(WardrobeItem item)
    {
        // Deduplicate by design identifier if available
        // Register item with server.
        // If it is valid, the server will send the item back via a push command
        _ = SyncItemToServer(item);
    }

    public async Task<GlamourerItem?> GetGlamourSlotFromPlayer(GlamourerEquipmentSlot slot)
    {
        var designJson = await _glamourerService.GetDesignComponentsAsync(
            GlamourerService.PLAYER_ID
        );
        if (designJson is not JObject jObject)
        {
            return null;
        }

        var glamourerDesign = GlamourerDesignHelper.FromJObject(jObject);
        if (glamourerDesign == null)
        {
            return null;
        }

        var item = slot switch
        {
            GlamourerEquipmentSlot.Head => glamourerDesign.Equipment.Head,
            GlamourerEquipmentSlot.Body => glamourerDesign.Equipment.Body,
            GlamourerEquipmentSlot.Hands => glamourerDesign.Equipment.Hands,
            GlamourerEquipmentSlot.Legs => glamourerDesign.Equipment.Legs,
            GlamourerEquipmentSlot.Feet => glamourerDesign.Equipment.Feet,
            GlamourerEquipmentSlot.Ears => glamourerDesign.Equipment.Ears,
            GlamourerEquipmentSlot.Neck => glamourerDesign.Equipment.Neck,
            GlamourerEquipmentSlot.Wrists => glamourerDesign.Equipment.Wrists,
            GlamourerEquipmentSlot.RFinger => glamourerDesign.Equipment.RFinger,
            GlamourerEquipmentSlot.LFinger => glamourerDesign.Equipment.LFinger,
            _ => null,
        };

        return item;
    }

    public async Task<GlamourerDesign?> GetDesignAsync(Guid designId)
    {
        var designJson = await _glamourerService.GetDesignJObjectAsync(designId);
        if (designJson is not JObject jObject)
        {
            return null;
        }
        var glamourerDesign = GlamourerDesignHelper.FromJObject(jObject);
        if (glamourerDesign == null)
        {
            return null;
        }

        return glamourerDesign;
    }

    public void Dispose()
    {
        _glamourerService.IpcReady -= OnIpcReady;
        _penumbraService.IpcReady -= OnPenumbraIpcReady;
        _penumbraService.ClearAllTemporaryMods();
        GC.SuppressFinalize(this);
    }
}
