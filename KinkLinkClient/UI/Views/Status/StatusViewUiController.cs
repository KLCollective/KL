using System;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Honorific.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Handlers;
using KinkLinkClient.Services;
using KinkLinkClient.UI.Components.Input;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.Status;

public class StatusViewUiController(
    NetworkService networkService,
    IdentityService identityService,
    GlamourerService glamourer,
    CustomizePlusService customizePlus,
    HonorificService honorific,
    PenumbraService penumbra,
    PermanentTransformationHandler permanentTransformationHandler,
    WardrobeManager wardrobeManager,
    LockService lockService,
    WardrobeNetworkService wardrobeNetworkService
)
{
    public readonly FourDigitInput PinInput = new("StatusInput");
    public GlamourerDesign? BaseLayer => wardrobeManager.ActiveSet.GetBaseLayer();

    public void RemoveBaseSet() =>
        _ = wardrobeNetworkService.ClearActiveWardrobeLayerAsync(WardrobeLayer.Outfit);

    public void RemoveSlotItem(GlamourerEquipmentSlot slot)
    {
        var layer = slot switch
        {
            GlamourerEquipmentSlot.Head => WardrobeLayer.Head,
            GlamourerEquipmentSlot.Body => WardrobeLayer.Chest,
            GlamourerEquipmentSlot.Hands => WardrobeLayer.Hands,
            GlamourerEquipmentSlot.Legs => WardrobeLayer.Legs,
            GlamourerEquipmentSlot.Feet => WardrobeLayer.Feet,
            GlamourerEquipmentSlot.Ears => WardrobeLayer.Ears,
            GlamourerEquipmentSlot.Neck => WardrobeLayer.Neck,
            GlamourerEquipmentSlot.Wrists => WardrobeLayer.Wrists,
            GlamourerEquipmentSlot.RFinger => WardrobeLayer.RFinger,
            GlamourerEquipmentSlot.LFinger => WardrobeLayer.LFinger,
            _ => WardrobeLayer.Outfit,
        };
        _ = wardrobeManager.RemovePieceFromSlotAsync(layer);
    }

    public WardrobeItem? GetEquipmentSlot(GlamourerEquipmentSlot slot)
    {
        var layer = slot switch
        {
            GlamourerEquipmentSlot.Head => WardrobeLayer.Head,
            GlamourerEquipmentSlot.Body => WardrobeLayer.Chest,
            GlamourerEquipmentSlot.Hands => WardrobeLayer.Hands,
            GlamourerEquipmentSlot.Legs => WardrobeLayer.Legs,
            GlamourerEquipmentSlot.Feet => WardrobeLayer.Feet,
            GlamourerEquipmentSlot.Ears => WardrobeLayer.Ears,
            GlamourerEquipmentSlot.Neck => WardrobeLayer.Neck,
            GlamourerEquipmentSlot.Wrists => WardrobeLayer.Wrists,
            GlamourerEquipmentSlot.RFinger => WardrobeLayer.RFinger,
            GlamourerEquipmentSlot.LFinger => WardrobeLayer.LFinger,
            _ => WardrobeLayer.Outfit,
        };

        return wardrobeManager.ActiveSet.GetIndividual(layer);
    }

    public void UnlockWardrobeSlot(string slotName)
    {
        // TODO: Implement wardrobe unlock via network
    }

    public LockInfoDto? GetLock(string lockId) => lockService.GetLock(lockId);

    /// <summary>
    ///     Attempt to unlock the client's appearance
    /// </summary>
    public void Unlock() =>
        permanentTransformationHandler.TryClearPermanentTransformation(PinInput.Value);

    /// <summary>
    ///     Button event to trigger a server disconnect
    /// </summary>
    public async void Disconnect()
    {
        try
        {
            await networkService.StopAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Plugin.Log.Warning(
                $"[StatusViewUiController] Unable to disconnect from the server, {e.Message}"
            );
        }
    }

    /// <summary>
    ///     Button event to trigger an identity reset
    /// </summary>
    public async void ResetIdentity()
    {
        try
        {
            if (await glamourer.RevertToAutomation(0).ConfigureAwait(false) is false)
                return;

            identityService.ClearAlterations();
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[StatusViewUiController] Unable to reset identity, {e.Message}");
        }
    }

    public async void ResetHonorific()
    {
        try
        {
            await honorific.ClearCharacterTitle(0).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    public async void ResetCollection()
    {
        try
        {
            var guid = await penumbra.GetCollection().ConfigureAwait(false);
            await penumbra.CallRemoveTemporaryMod(guid).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    public async void ResetCustomize()
    {
        try
        {
            await customizePlus.DeleteTemporaryCustomizeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignored
        }
    }
}
