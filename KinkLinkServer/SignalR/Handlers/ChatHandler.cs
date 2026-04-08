using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Handles logic for fulfilling a <see cref="SendGlobalMessageRequest"/>
/// </summary>
public class ChatHandler(ILogger<ChatHandler> logger)
{
    private int maxMessageLength = 500;

    /// <summary>
    ///     Handles sending a global chat message
    /// </summary>
    public async Task<ChatSendMessageResponse> HandleSendMessage(
        string profileUID,
        ChatSendMessageRequest request,
        IHubCallerClients clients
    )
    {
        // profileUID is guaranteed to be from authenticated user (passed from hub)

        // Validate message
        if (
            string.IsNullOrWhiteSpace(request.Message)
            || request.Message.Length > maxMessageLength
            || request.Message.Contains('\n')
        )
        {
            logger.LogWarning("{Sender} sent invalid chat message somehow", profileUID);
            return new ChatSendMessageResponse(
                ChatSendMessageEc.InvalidMessage,
                "Chat message either contained escape characters, was empty or exceeded 500 characters"
            );
        }

        // Get user alias
        var user_alias = GetAlias(profileUID, request.Title);
        var dto = new ChatReceivedMessage(user_alias, request.Message, DateTime.UtcNow);

        // Broadcast to all authenticated users
        await clients.All.SendAsync(HubMethod.ReceiveChatMessage, dto);

        logger.LogInformation(
            "{Sender} ({Alias}) sent global message: {Message}",
            profileUID,
            user_alias,
            request.Message
        );
        return new ChatSendMessageResponse(ChatSendMessageEc.Success, "");
    }

    private string GetAlias(string uid, Title title)
    {
        var titleStr = title.ToString();
        var uidSuffix = uid.Length >= 4 ? uid[^4..] : uid.PadLeft(4, '0');
        return $"{titleStr}#{uidSuffix}";
    }
}
