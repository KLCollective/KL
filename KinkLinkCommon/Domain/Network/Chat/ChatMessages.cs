using MessagePack;

namespace KinkLinkCommon.Domain.Network;

// These are valid titles that users can choose when joining global chat.
// This is set in the profile settings.
// TODO: Consider moving this to a more appropriate location or keeping here.
public enum Title
{
    Kinkster,
    Submissive,
    Slave,
    Doll,
    Brat,
    Switch,
    Miss,
    Sir,
    Mistress,
    Master,
}

/// The return stats for the chat message request
public enum ChatSendMessageEc
{
    InvalidMessage,
    Unauthorized,
    ServerError,
    Success,
}

/// This is sent from the client to the server for chat messages.
[MessagePackObject]
public record ChatSendMessageRequest(
    [property: Key(0)] Title Title,
    [property: Key(1)] string Message
);

[MessagePackObject]
public record ChatSendMessageResponse(
    [property: Key(0)] ChatSendMessageEc Status,
    [property: Key(1)] string Error
);

/// This DTO is sent from the server to the client for chat messages
[MessagePackObject]
public record ChatReceivedMessage(
    [property: Key(0)] string Alias,
    [property: Key(1)] string Message,
    [property: Key(2)] DateTime Timestamp
);
