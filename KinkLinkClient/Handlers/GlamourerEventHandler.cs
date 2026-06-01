using System;
using System.Timers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Api.IpcSubscribers;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Domain.Events;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;

namespace KinkLinkClient.Handlers;

public class GlamourerEventHandler : IDisposable
{
    private readonly GlamourerService _glamourerService;
    private readonly WardrobeManager _wardrobeManager;
    private bool handlingStateChanged = false;
    private bool handlingStateFinalized = false;

    public GlamourerEventHandler(GlamourerService glamourerService, WardrobeManager wardrobeManager)
    {
        _glamourerService = glamourerService;
        _wardrobeManager = wardrobeManager;

        _glamourerService.OnStateChangedWithType.Event += OnStateChangedWithType;
        _glamourerService.OnStateFinalizedWithType.Event += OnStateFinalizedWithType;
    }

    private unsafe bool isLocalPlayer(nint address)
    {
        return address == (nint)Control.Instance()->LocalPlayer;
    }

    public async void OnStateChangedWithType(
        nint address,
        Glamourer.Api.Enums.StateChangeType state
    )
    {
        // Plugin.Log.Info(
        //     $"OnStateChangedWithType: Object {address} has new {state} and we are already handling: {handlingStateChanged}"
        // );
        if (!isLocalPlayer(address) || handlingStateChanged)
            return;
        // Simply mutex lock to ensure that it doesn't infinitely recurse
        handlingStateChanged = true;
        try
        {
            var jobject = await _glamourerService.GetDesignComponentsAsync(GlamourerService.PLAYER_ID);
            var design = GlamourerDesignHelper.FromJObject(jobject);
            if (design != null)
                await _wardrobeManager.ReapplyIfChanged(design);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[GlamourerEventHandler] Error in OnStateChangedWithType");
        }
        finally
        {
            handlingStateChanged = false;
        }
    }

    public async void OnStateFinalizedWithType(
        nint address,
        Glamourer.Api.Enums.StateFinalizationType state
    )
    {
        // Plugin.Log.Info(
        //     $"OnStateFinalizedWithType: Object {address} has new {state} and we are already handling: {handlingStateFinalized}"
        // );
        // Ignore everything that isn't the local player
        if (!isLocalPlayer(address) || handlingStateFinalized)
            return;
        // Simply mutex lock to ensure that it doesn't infinitely recurse
        handlingStateFinalized = true;
        var jobject = await _glamourerService.GetDesignComponentsAsync(GlamourerService.PLAYER_ID);
        var design = GlamourerDesignHelper.FromJObject(jobject);
        if (design != null)
            await _wardrobeManager.ReapplyIfChanged(design);
        handlingStateFinalized = false;
    }

    /// <summary>
    ///     Tests to see if any equipment marked with 'apply' are different
    /// </summary>
    public void Dispose()
    {
        _glamourerService.OnStateChangedWithType.Event -= OnStateChangedWithType;
        _glamourerService.OnStateFinalizedWithType.Event -= OnStateFinalizedWithType;
        GC.SuppressFinalize(this);
    }
}
