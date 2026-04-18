using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Customize;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

public class CustomizePlusHandler(IPresenceService presenceService, IForwardedRequestManager forwardedRequestManager, ILogger<CustomizePlusHandler> logger)
{
    private const string Method = HubMethod.CustomizePlus;
    private static readonly UserPermissions Permissions = new();

    /// <summary>
    ///     Handles the request
    /// </summary>
    public async Task<ActionResponse> Handle(string senderFriendCode, CustomizeRequest request, IHubCallerClients clients)
    {
        logger.LogDebug("Customize+ request from {From}, targets: {Targets}", senderFriendCode, request.TargetFriendCodes.Count);

        if (ValidateCustomizeRequest(senderFriendCode, request) is { } error)
        {
            logger.LogWarning("{Sender} sent invalid customize+ request {Error}", senderFriendCode, error);
            return new ActionResponse(error, []);
        }

        var forwardedRequest = new CustomizeCommand(senderFriendCode, request.JsonBoneDataBytes);
        return await forwardedRequestManager.CheckPermissionsAndSend(senderFriendCode, request.TargetFriendCodes, Method, Permissions, forwardedRequest, clients);
    }

    private ActionResponseEc? ValidateCustomizeRequest(string senderFriendCode, CustomizeRequest request)
    {
        if (presenceService.IsUserExceedingCooldown(senderFriendCode))
            return ActionResponseEc.TooManyRequests;

        if (VerificationUtilities.ValidListOfFriendCodes(request.TargetFriendCodes) is false)
            return ActionResponseEc.BadDataInRequest;

        if (VerificationUtilities.IsJsonBytes(request.JsonBoneDataBytes) is false)
            return ActionResponseEc.BadDataInRequest;

        return null;
    }
}
