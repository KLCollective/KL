using System;
using System.Text;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Handlers.Network.Base;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Customize;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

/// <summary>
///     Handles a <see cref="CustomizeCommand"/>
/// </summary>
public class CustomizePlusHandler : AbstractNetworkHandler, IDisposable
{
    // Const
    private const string Operation = "Customize+";
    private static readonly UserPermissions Permissions = new();

    // Injected
    private readonly CustomizePlusService _customize;
    private readonly LogService _log;

    // Instantiated
    private readonly IDisposable _handler;

    /// <summary>
    ///     <inheritdoc cref="CustomizePlusHandler"/>
    /// </summary>
    public CustomizePlusHandler(
        CustomizePlusService customize,
        FriendsListService friends,
        LogService log,
        NetworkService network,
        PauseService pause
    )
        : base(friends, log, pause)
    {
        _customize = customize;
        _log = log;

        _handler = network.Connection.On<CustomizeCommand, ActionResult<Unit>>(
            HubMethod.CustomizePlus,
            Handle
        );
    }

    /// <summary>
    ///     <inheritdoc cref="MoodlesHandler"/>
    /// </summary>
    private async Task<ActionResult<Unit>> Handle(CustomizeCommand request)
    {
        var sender = TryGetFriendWithCorrectPermissions(
            Operation,
            request.SenderFriendCode,
            Permissions
        );
        if (sender.Result is not ActionResultEc.Success)
            return ActionResultBuilder.Fail(sender.Result);

        if (sender.Value is not { } friend)
            return ActionResultBuilder.Fail(ActionResultEc.ValueNotSet);

        try
        {
            var json = Encoding.UTF8.GetString(request.JsonBoneDataBytes);

            bool success = request.ApplyMode switch
            {
                CustomizeApplyMode.Merge =>
                    await _customize.ApplyMergeCustomizeAsync(json).ConfigureAwait(false),
                _ => // Default and Uninitialized
                    await _customize.DeleteTemporaryCustomizeAsync().ConfigureAwait(false) &&
                    await _customize.ApplyCustomizeAsync(json).ConfigureAwait(false)
            };

            if (success is false)
            {
                Plugin.Log.Warning($"[CustomizePlusHandler] Unable to apply customize (mode: {request.ApplyMode})");
                return ActionResultBuilder.Fail(ActionResultEc.ClientPluginDependency);
            }

            _log.Custom($"{friend.NoteOrFriendCode} applied a customize plus template to you (mode: {request.ApplyMode})");
            return ActionResultBuilder.Ok();
        }
        catch (Exception e)
        {
            _log.Custom(
                $"{friend.NoteOrFriendCode} tried to apply a customization template to you but failed unexpectedly"
            );
            Plugin.Log.Error(
                $"Unexpected exception while handling customize plus action, {e.Message}"
            );
            return ActionResultBuilder.Fail(ActionResultEc.Unknown);
        }
    }

    public void Dispose()
    {
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }
}
