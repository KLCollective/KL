using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("[SignalR] CustomizePlus: {FriendCode}, Targets: {Targets}", FriendCode, string.Join(", ", request.TargetFriendCodes));
            return await customizePlusHandler.Handle(FriendCode, request, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("CustomizePlus", true);
            metricsService.RecordSignalRMessageDuration("CustomizePlus", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.Honorific)]
    public async Task<ActionResponse> Honorific(HonorificRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogInformation("[SignalR] Honorific: {FriendCode}, Targets: {Targets}, Honorific: {Honorific}", friendCode, string.Join(", ", request.TargetFriendCodes), request.Honorific);
            LogWithBehavior($"[HonorificRequest] Sender = {friendCode}, Targets = {string.Join(", ", request.TargetFriendCodes)}, Honorific = {request.Honorific}", LogMode.Console);
            return await honorificHandler.Handle(friendCode, request, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("Honorific", true);
            metricsService.RecordSignalRMessageDuration("Honorific", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.Moodles)]
    public async Task<ActionResponse> GetMoodlesAction(MoodlesRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("[SignalR] Moodles: {FriendCode}, Targets: {Targets}", FriendCode, string.Join(", ", request.TargetFriendCodes));
            return await moodlesHandler.Handle(FriendCode, request, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("Moodles", true);
            metricsService.RecordSignalRMessageDuration("Moodles", stopwatch.ElapsedMilliseconds);
        }
    }
}