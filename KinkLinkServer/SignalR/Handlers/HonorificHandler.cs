using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Honorific;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Handles the logic for fulling a <see cref="HonorificRequest"/>
/// </summary>
public class HonorificHandler(IPresenceService presenceService, IForwardedRequestManager forwardedRequest, ILogger<MoodlesHandler> logger)
{
    private const string Method = HubMethod.Honorific;
    private static readonly UserPermissions Permissions = new();

    public async Task<ActionResponse> Handle(string senderFriendCode, HonorificRequest request, IHubCallerClients clients)
    {
        logger.LogDebug("Honorific request from {From}, targets: {Targets}", senderFriendCode, request.TargetFriendCodes.Count);

        if (ValidateHonorificRequest(senderFriendCode, request) is { } error)
        {
            logger.LogWarning("{Sender} sent invalid speak request {Error}", senderFriendCode, error);
            return new ActionResponse(error, []);
        }

        var command = new HonorificCommand(senderFriendCode, request.Honorific);
        return await forwardedRequest.CheckPermissionsAndSend(senderFriendCode, request.TargetFriendCodes, Method, Permissions, command, clients);
    }

    private ActionResponseEc? ValidateHonorificRequest(string senderFriendCode, HonorificRequest request)
    {
        if (presenceService.IsUserExceedingCooldown(senderFriendCode))
            return ActionResponseEc.TooManyRequests;

        if (VerificationUtilities.ValidListOfFriendCodes(request.TargetFriendCodes) is false)
            return ActionResponseEc.BadDataInRequest;

        // TODO: Define rules for validating Honorific data

        return null;
    }
}
