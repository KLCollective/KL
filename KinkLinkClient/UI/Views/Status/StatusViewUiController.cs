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

namespace KinkLinkClient.UI.Views.Status;

public class StatusViewUiController(
    NetworkService networkService,
    IdentityService identityService,
    GlamourerService glamourer,
    CustomizePlusService customizePlus,
    HonorificService honorific,
    PenumbraService penumbra,
    PermanentTransformationHandler permanentTransformationHandler,
    WardrobeService wardrobeService,
    LockService lockService
)
{
    public readonly FourDigitInput PinInput = new("StatusInput");
    public GlamourerDesign? BaseLayer => wardrobeService.ActiveSet.GetBaseLayer();

    public void RemoveBaseSet() => _ = wardrobeService.RemoveActiveSetAsync();

    public void RemoveSlotItem(GlamourerEquipmentSlot slot) =>
        _ = wardrobeService.RemovePieceFromSlotAsync(slot);

    public WardrobeItem? GetEquipmentSlot(GlamourerEquipmentSlot slot) =>
        wardrobeService.ActiveSet.GetIndividual(slot);

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
