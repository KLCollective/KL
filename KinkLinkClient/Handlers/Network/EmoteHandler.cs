using System;
using System.Text;
using KinkLinkClient.Handlers.Network.Base;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Emote;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

/// <summary>
///     Handles a <see cref="EmoteCommand"/>
/// </summary>
public class EmoteHandler : AbstractNetworkHandler, IDisposable
{
    // Const
    private const string Operation = "Emote";
    private static readonly UserPermissions Permissions = new();

    // Injected
    private readonly EmoteService _emote;
    private readonly LogService _log;

    // Instantiated
    private readonly IDisposable _handler;

    /// <summary>
    ///     <inheritdoc cref="EmoteHandler"/>
    /// </summary>
    public EmoteHandler(
        EmoteService emote,
        FriendsListService friends,
        LogService log,
        NetworkService network,
        PauseService pause
    )
        : base(friends, log, pause)
    {
        _emote = emote;
        _log = log;

        _handler = network.Connection.On<EmoteCommand, ActionResult<Unit>>(HubMethod.Emote, Handle);
    }

    /// <summary>
    ///     <inheritdoc cref="EmoteHandler"/>
    /// </summary>
    private ActionResult<Unit> Handle(EmoteCommand request)
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

        // Check if real emote
        if (_emote.Emotes.Contains(request.Emote) is false)
        {
            _log.InvalidData(Operation, friend.NoteOrFriendCode);
            return ActionResultBuilder.Fail(ActionResultEc.ClientBadData);
        }

        // Construct command
        var command = new StringBuilder();
        command.Append('/');
        command.Append(request.Emote);
        if (request.DisplayLogMessage is false)
            command.Append(" <mo>");

        // Execute command
        ChatService.SendMessage(command.ToString());

        // Log success
        _log.Custom($"{friend.NoteOrFriendCode} made you do the {request.Emote} emote");

        // Success
        return ActionResultBuilder.Ok();
    }

    public void Dispose()
    {
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }
}
