namespace KinkLinkCommon.Domain.Network;

/// <summary>
///     The names of the hub methods the server / client can call
/// </summary>
public static class HubMethod
{
    public const string AddFriend = "AddFriend";
    public const string UpdateFriend = "UpdateFriend";
    public const string RemoveFriend = "RemoveFriend";

    public const string AddWardrobeItem = "AddWardrobeItem";
    public const string RemoveWardrobeItem = "RemoveWardrobeItem";
    public const string GetWardrobeItem = "GetWardrobeItem";
    public const string ListWardrobeItems = "ListWardrobeItems";

    public const string SetWardrobeStatus = "SetWardrobeStatus";
    public const string GetWardrobeStatus = "GetWardrobeStatus";

    public const string Speak = "Speak";
    public const string Emote = "Emote";
    public const string Transform = "Transform";
    public const string BodySwap = "BodySwap";
    public const string Twinning = "Twinning";

    public const string GetAccountData = "GetAccountData";

    public const string SyncOnlineStatus = "SyncOnlineStatus";
    public const string SyncPermissions = "SyncPermissions";

    public const string Moodles = "Moodles";
    public const string CustomizePlus = "CustomizePlus";
    public const string Hypnosis = "Hypnosis";
    public const string HypnosisStop = "HypnosisStop";
    public const string Honorific = "Honorific";

    public const string SendChatMessage = "SendChatMessage";
    public const string ReceiveChatMessage = "ReceiveChatMessage";

    public const string QueryPairState = "QueryPairState";
    public const string QueryPairWardrobeState = "QueryPairWardrobeState";
    public const string QueryPairWardrobe = "QueryPairWardrobe";
    public const string ApplyInteraction = "ApplyInteraction";

    public const string SyncLocks = "SyncLocks";
    public const string AddLock = "AddLock";
    public const string RemoveLock = "RemoveLock";

    public const string GetProfile = "GetProfile";
    public const string UpdateProfile = "UpdateProfile";
    public const string GetProfileConfig = "GetProfileConfig";
    public const string UpdateProfileConfig = "UpdateProfileConfig";

    public const string SyncPairState = "SyncPairState";
    public const string SyncWardrobeState = "SyncWardrobeState";
    public const string RequestInitialState = "RequestInitialState";
}
