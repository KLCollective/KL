using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Emote;
using KinkLinkCommon.Domain.Network.Speak;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.Speak)]
    public async Task<ActionResponse> Speak(SpeakRequest request)
    {
        var friendCode = FriendCode;
        logger.LogInformation("[SignalR] Speak: {FriendCode}, Targets: {Targets}", friendCode, string.Join(", ", request.TargetFriendCodes));
        LogWithBehavior($"[SpeakRequest] Sender = {friendCode}, Targets = {string.Join(", ", request.TargetFriendCodes)}, Message = {request.Message}", LogMode.Both);
        return await speakHandler.Handle(friendCode, request, Clients);
    }

    [HubMethodName(HubMethod.Emote)]
    public async Task<ActionResponse> Emote(EmoteRequest request)
    {
        logger.LogTrace("[SignalR] Emote: {FriendCode}, Targets: {Targets}", FriendCode, string.Join(", ", request.TargetFriendCodes));
        return await emoteHandler.Handle(FriendCode, request, Clients);
    }
}
