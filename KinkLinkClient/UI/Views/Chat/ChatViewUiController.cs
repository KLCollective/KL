using System;
using System.Collections.Generic;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Network;

namespace KinkLinkClient.UI.Views.Chat;

public class ChatViewUiController(
    NetworkService network,
    WorldService world,
    ProfileService profile
) : IDisposable
{
    public bool IsBusy => _busy;
    public string InputMessage = string.Empty;
    public bool ScrollToBottom = true;

    // Injected
    private readonly NetworkService _network = network;
    private readonly WorldService _world = world;
    private readonly ProfileService _profile = profile;

    private const uint MAX_CHAT_HISTORY = 100;
    private List<ChatReceivedMessage> _messages = new();

    private bool _busy = false;

    public async void SendChat()
    {
        // Can only have one request in flight
        // Sends the `ChatMessage` to the server via the `ChatSentMessage` DTO
        if (
            _busy
            || string.IsNullOrWhiteSpace(this.InputMessage)
            || _profile.CurrentProfile is null
        )
        {
            return;
        }

        try
        {
            // Ensure we only send one at a time.
            _busy = true;
            var dto = new ChatSendMessageRequest(_profile.CurrentProfile.Title, this.InputMessage);
            this.InputMessage = string.Empty;
            var response = await _network
                .InvokeAsync<ChatSendMessageResponse>(HubMethod.SendChatMessage, dto)
                .ConfigureAwait(false);
            if (response.Status is not ChatSendMessageEc.Success)
            {
                NotificationHelper.Info(
                    "Chat Send Unsuccessful",
                    string.Format(
                        "An error has occurred when sending this chat message {0}",
                        response.Error
                    )
                );
            }
            // Assuming no issues we can just clear this safely for the next messageV
            _busy = false;
        }
        catch
        {
            // Ignored, but do not delete the message
            return;
        }
    }

    public IEnumerable<ChatReceivedMessage> Messages()
    {
        return _messages;
    }

    public void AddMessage(ChatReceivedMessage message)
    {
        // TODO: If this proves too costly, migrate to a circular buffer
        // To keep the chat reasonably optimized, start removing messages if it is too long.
        if (_messages.Count > MAX_CHAT_HISTORY)
        {
            _messages.RemoveAt(0);
        }
        _messages.Add(message);
        ScrollToBottom = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
