using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogInformation("[SignalR] Speak: {FriendCode}, Targets: {Targets}", friendCode, string.Join(", ", request.TargetFriendCodes));
            LogWithBehavior($"[SpeakRequest] Sender = {friendCode}, Targets = {string.Join(", ", request.TargetFriendCodes)}, Message = {request.Message}", LogMode.Both);
            return await speakHandler.Handle(friendCode, request, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("Speak", true);
            metricsService.RecordSignalRMessageDuration("Speak", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.Emote)]
    public async Task<ActionResponse> Emote(EmoteRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] Emote: {FriendCode}, Targets: {Targets}", FriendCode, string.Join(", ", request.TargetFriendCodes));
            return await emoteHandler.Handle(FriendCode, request, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("Emote", true);
            metricsService.RecordSignalRMessageDuration("Emote", stopwatch.ElapsedMilliseconds);
        }
    }
}