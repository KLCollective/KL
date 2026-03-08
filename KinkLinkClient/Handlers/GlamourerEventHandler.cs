using System;
using System.Timers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Api.IpcSubscribers;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkClient.Domain.Events;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

namespace KinkLinkClient.Handlers;

public class GlamourerEventHandler : IDisposable
{
    private readonly GlamourerService _glamourerService;
    private readonly WardrobeService _wardrobeService;
    private bool handlingStateChanged = false;
    private bool handlingStateFinalized = false;

    public GlamourerEventHandler(GlamourerService glamourerService, WardrobeService wardrobeService)
    {
        _glamourerService = glamourerService;
        _wardrobeService = wardrobeService;

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
        Plugin.Log.Info(
            $"OnStateChangedWithType: Object {address} has new {state} and we are already handling: {handlingStateChanged}"
        );
        if (
            state is Glamourer.Api.Enums.StateChangeType.Equip
            || state is Glamourer.Api.Enums.StateChangeType.Stains
        )
            if (!isLocalPlayer(address) || handlingStateChanged)
                return;
        // Simply mutex lock to ensure that it doesn't infinitely recurse
        handlingStateChanged = true;

        var jobject = await _glamourerService.GetDesignComponentsAsync(GlamourerService.PLAYER_ID);
        var design = GlamourerDesignHelper.FromJObject(jobject);
        if (design != null)
            await _wardrobeService.ReapplyIfChanged(design);

        handlingStateChanged = false;
    }

    public async void OnStateFinalizedWithType(
        nint address,
        Glamourer.Api.Enums.StateFinalizationType state
    )
    {
        Plugin.Log.Info(
            $"OnStateFinalizedWithType: Object {address} has new {state} and we are already handling: {handlingStateFinalized}"
        );
        // Ignore everything that isn't the local player
        if (!isLocalPlayer(address) || handlingStateChanged)
            return;
        // Simply mutex lock to ensure that it doesn't infinitely recurse
        handlingStateFinalized = true;
        var jobject = await _glamourerService.GetDesignComponentsAsync(GlamourerService.PLAYER_ID);
        var design = GlamourerDesignHelper.FromJObject(jobject);
        if (design != null)
            await _wardrobeService.ReapplyIfChanged(design);
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
