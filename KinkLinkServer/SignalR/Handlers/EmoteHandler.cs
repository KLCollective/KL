using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Emote;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Utilities;
using Microsoft.AspNetCore.SignalR;
using Constraints = KinkLinkCommon.Constraints;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Handles the logic for fulfilling a <see cref="EmoteRequest"/>
/// </summary>
public class EmoteHandler(IPresenceService presenceService, IForwardedRequestManager forwardedRequestManager, ILogger<EmoteHandler> logger)
{
    private const string Method = HubMethod.Emote;
    private static readonly UserPermissions Permissions = new();

    /// <summary>
    ///     Handles the request
    /// </summary>
    public async Task<ActionResponse> Handle(string senderFriendCode, EmoteRequest request, IHubCallerClients clients)
    {
        logger.LogDebug("Emote request from {From}, targets: {Targets}, emote: {Emote}", senderFriendCode, request.TargetFriendCodes.Count, request.Emote);

        if (ValidateEmoteRequest(senderFriendCode, request) is { } error)
        {
            logger.LogWarning("{Sender} sent invalid speak request {Error}", senderFriendCode, error);
            return new ActionResponse(error, []);
        }

        var command = new EmoteCommand(senderFriendCode, request.Emote, request.DisplayLogMessage);
        return await forwardedRequestManager.CheckPermissionsAndSend(senderFriendCode, request.TargetFriendCodes, Method, Permissions, command, clients);
    }

    private ActionResponseEc? ValidateEmoteRequest(string senderFriendCode, EmoteRequest request)
    {
        if (presenceService.IsUserExceedingCooldown(senderFriendCode))
            return ActionResponseEc.TooManyRequests;

        if (VerificationUtilities.ValidListOfFriendCodes(request.TargetFriendCodes) is false)
            return ActionResponseEc.BadDataInRequest;

        if (request.TargetFriendCodes.Count > Constraints.MaximumTargetsForInGameOperations)
            return ActionResponseEc.TooManyTargets;

        return null;
    }
}
