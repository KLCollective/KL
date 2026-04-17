using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Customize;
using KinkLinkCommon.Domain.Network.Honorific;
using KinkLinkCommon.Domain.Network.Moodles;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.CustomizePlus)]
    public async Task<ActionResponse> CustomizePlus(CustomizeRequest request)
    {
        logger.LogInformation("[SignalR] CustomizePlus: {FriendCode}, Targets: {Targets}", FriendCode, string.Join(", ", request.TargetFriendCodes));
        return await customizePlusHandler.Handle(FriendCode, request, Clients);
    }

    [HubMethodName(HubMethod.Honorific)]
    public async Task<ActionResponse> Honorific(HonorificRequest request)
    {
        var friendCode = FriendCode;
        logger.LogInformation("[SignalR] Honorific: {FriendCode}, Targets: {Targets}, Honorific: {Honorific}", friendCode, string.Join(", ", request.TargetFriendCodes), request.Honorific);
        LogWithBehavior($"[HonorificRequest] Sender = {friendCode}, Targets = {string.Join(", ", request.TargetFriendCodes)}, Honorific = {request.Honorific}", LogMode.Console);
        return await honorificHandler.Handle(friendCode, request, Clients);
    }

    [HubMethodName(HubMethod.Moodles)]
    public async Task<ActionResponse> GetMoodlesAction(MoodlesRequest request)
    {
        logger.LogInformation("[SignalR] Moodles: {FriendCode}, Targets: {Targets}", FriendCode, string.Join(", ", request.TargetFriendCodes));
        return await moodlesHandler.Handle(FriendCode, request, Clients);
    }
}
