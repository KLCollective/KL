using KinkLinkCommon;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Moodles;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Handles the logic for fulling a <see cref="MoodlesRequest"/>
/// </summary>
public class MoodlesHandler(IPresenceService presenceService, IForwardedRequestManager forwardedRequestManager, ILogger<MoodlesHandler> logger)
{
    private const string Method = HubMethod.Moodles;
    private static readonly UserPermissions Permissions = new();

    /// <summary>
    ///     Handles the request
    /// </summary>
    public async Task<ActionResponse> Handle(string senderFriendCode, MoodlesRequest request, IHubCallerClients clients)
    {
        logger.LogDebug("Moodles request from {From}, targets: {Targets}", senderFriendCode, request.TargetFriendCodes.Count);

        if (ValidateEmoteRequest(senderFriendCode, request) is { } error)
        {
            logger.LogWarning("{Sender} sent invalid moodles request {Error}", senderFriendCode, error);
            return new ActionResponse(error, []);
        }

        var command = new MoodlesCommand(senderFriendCode, request.Info);
        return await forwardedRequestManager.CheckPermissionsAndSend(senderFriendCode, request.TargetFriendCodes, Method, Permissions, command, clients);
    }

    private ActionResponseEc? ValidateEmoteRequest(string senderFriendCode, MoodlesRequest request)
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
