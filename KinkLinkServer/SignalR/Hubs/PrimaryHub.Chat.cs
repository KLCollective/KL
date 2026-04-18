using System.Diagnostics;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.SendChatMessage)]
    public async Task<ChatSendMessageResponse> SendChatMessage(ChatSendMessageRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("[SignalR] SendChatMessage: {FriendCode}, Title: {Title}",
                FriendCode, request.Title);
            LogWithBehavior($"[Chat_SendMessage] Sender = {FriendCode}, Message = {request.Message?.Substring(0, Math.Min(50, request.Message?.Length ?? 0))}", LogMode.Both);
            return await chatHandler.HandleSendMessage(FriendCode, request, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("SendChatMessage", true);
            metricsService.RecordSignalRMessageDuration("SendChatMessage", stopwatch.ElapsedMilliseconds);
        }
    }
}