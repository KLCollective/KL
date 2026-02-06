using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums.Permissions;
using MessagePack;

namespace KinkLinkCommon.Domain;

[MessagePackObject(keyAsPropertyName: true)]
public record UserGarblerSettings
{
    public bool GarblerEnabled, GarblerLocked, GarblerChannelsLocked;
    public GarblerChannels Channels;
    public UserGarblerSettings()
    {
        GarblerEnabled = false;
        GarblerLocked = false;
        GarblerChannelsLocked = false;
        Channels = GarblerChannels.None;
    }
    /// <summary>
    ///     Creates garbler settings from a pair's permissions
    /// </summary>
    public UserGarblerSettings(Pair pair)
    {
        // Initialize with default values
        GarblerEnabled = false;
        GarblerLocked = false;
        GarblerChannelsLocked = false;
        Channels = GarblerChannels.None;
        
        // TODO: Reimplement when the new bitmasks are enabled
        // This would extract garbler settings from the pair's permissions
    }
    public UserGarblerSettings(bool garblerEnabled, bool garblerLocked, bool garblerChannelsLocked, GarblerChannels channels) 
    {
        GarblerEnabled = garblerEnabled;
        GarblerLocked = garblerLocked;
        GarblerChannelsLocked = garblerChannelsLocked;
        Channels = channels;
    }
}
