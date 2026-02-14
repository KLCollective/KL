using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;

namespace KinkLinkClient.Domain;

// ReSharper disable ConvertIfStatementToReturnStatement

/// <summary>
///     Class representing the <see cref="UserGarblerSettings"/> object but as booleans for use with ImGui
/// </summary>
public class GarblerSettingsUiState
{
    public bool GarblerEnabled, GarblerLocked, GarblerChannelsLocked;
    // Speak garblerState.
    public bool Say, Yell, Shout, Tell, Party, Alliance, FreeCompany, PvPTeam, Echo, Roleplay;

    // Linkshell garblerState.
    public bool Ls1, Ls2, Ls3, Ls4, Ls5, Ls6, Ls7, Ls8, Cwl1, Cwl2, Cwl3, Cwl4, Cwl5, Cwl6, Cwl7, Cwl8;

    /// <summary>
    ///     Tests if this object is equal to another <see cref="GarblerUiState"/>
    /// </summary>
    public bool Equals(GarblerSettingsUiState other)
    {
        if (GarblerEnabled != other.GarblerEnabled) return false;
        if (GarblerLocked != other.GarblerLocked) return false;
        if (GarblerChannelsLocked != other.GarblerChannelsLocked) return false;

        // Speak garblerState.
        if (Say != other.Say) return false;
        if (Yell != other.Yell) return false;
        if (Shout != other.Shout) return false;
        if (Tell != other.Tell) return false;
        if (Party != other.Party) return false;
        if (Alliance != other.Alliance) return false;
        if (FreeCompany != other.FreeCompany) return false;
        if (PvPTeam != other.PvPTeam) return false;
        if (Echo != other.Echo) return false;
        if (Roleplay != other.Roleplay) return false;

        // Linkshell garblerState.
        if (Ls1 != other.Ls1) return false;
        if (Ls2 != other.Ls2) return false;
        if (Ls3 != other.Ls3) return false;
        if (Ls4 != other.Ls4) return false;
        if (Ls5 != other.Ls5) return false;
        if (Ls6 != other.Ls6) return false;
        if (Ls7 != other.Ls7) return false;
        if (Ls8 != other.Ls8) return false;
        if (Cwl1 != other.Cwl1) return false;
        if (Cwl2 != other.Cwl2) return false;
        if (Cwl3 != other.Cwl3) return false;
        if (Cwl4 != other.Cwl4) return false;
        if (Cwl5 != other.Cwl5) return false;
        if (Cwl6 != other.Cwl6) return false;
        if (Cwl7 != other.Cwl7) return false;
        if (Cwl8 != other.Cwl8) return false;

        return true;
    }

    /// <summary>
    ///     Converts a <see cref="UserGarblerSettings"/> into a <see cref="GarblerUiState"/>
    /// </summary>
    public static GarblerSettingsUiState From(UserGarblerSettings garblerState)
    {
        return new GarblerSettingsUiState
        {
            GarblerEnabled = garblerState.GarblerEnabled,
            GarblerLocked = garblerState.GarblerLocked,
            GarblerChannelsLocked = garblerState.GarblerChannelsLocked,

            // Speak Channels
            Say = (garblerState.Channels & GarblerChannels.Say) != 0,
            Yell = (garblerState.Channels & GarblerChannels.Yell) != 0,
            Shout = (garblerState.Channels & GarblerChannels.Shout) != 0,
            Tell = (garblerState.Channels & GarblerChannels.Tell) != 0,
            Party = (garblerState.Channels & GarblerChannels.Party) != 0,
            Alliance = (garblerState.Channels & GarblerChannels.Alliance) != 0,
            FreeCompany = (garblerState.Channels & GarblerChannels.FreeCompany) != 0,
            PvPTeam = (garblerState.Channels & GarblerChannels.PvPTeam) != 0,
            Echo = (garblerState.Channels & GarblerChannels.Echo) != 0,
            Roleplay = (garblerState.Channels & GarblerChannels.Roleplay) != 0,

            // Linkshell Channels
            Ls1 = (garblerState.Channels & GarblerChannels.Ls1) != 0,
            Ls2 = (garblerState.Channels & GarblerChannels.Ls2) != 0,
            Ls3 = (garblerState.Channels & GarblerChannels.Ls3) != 0,
            Ls4 = (garblerState.Channels & GarblerChannels.Ls4) != 0,
            Ls5 = (garblerState.Channels & GarblerChannels.Ls5) != 0,
            Ls6 = (garblerState.Channels & GarblerChannels.Ls6) != 0,
            Ls7 = (garblerState.Channels & GarblerChannels.Ls7) != 0,
            Ls8 = (garblerState.Channels & GarblerChannels.Ls8) != 0,
            Cwl1 = (garblerState.Channels & GarblerChannels.Cwl1) != 0,
            Cwl2 = (garblerState.Channels & GarblerChannels.Cwl2) != 0,
            Cwl3 = (garblerState.Channels & GarblerChannels.Cwl3) != 0,
            Cwl4 = (garblerState.Channels & GarblerChannels.Cwl4) != 0,
            Cwl5 = (garblerState.Channels & GarblerChannels.Cwl5) != 0,
            Cwl6 = (garblerState.Channels & GarblerChannels.Cwl6) != 0,
            Cwl7 = (garblerState.Channels & GarblerChannels.Cwl7) != 0,
            Cwl8 = (garblerState.Channels & GarblerChannels.Cwl8) != 0,
        };
    }

    /// <summary>
    ///     Converts a <see cref="GarblerUiState"/> to <see cref="UserGarblerSettings"/>
    /// </summary>
    public static UserGarblerSettings To(GarblerSettingsUiState garblerState)
    {
        var chatchannels = GarblerChannels.None;

        // Speak Permissions
        if (garblerState.Say) chatchannels |= GarblerChannels.Say;
        if (garblerState.Yell) chatchannels |= GarblerChannels.Yell;
        if (garblerState.Shout) chatchannels |= GarblerChannels.Shout;
        if (garblerState.Tell) chatchannels |= GarblerChannels.Tell;
        if (garblerState.Party) chatchannels |= GarblerChannels.Party;
        if (garblerState.Alliance) chatchannels |= GarblerChannels.Alliance;
        if (garblerState.FreeCompany) chatchannels |= GarblerChannels.FreeCompany;
        if (garblerState.PvPTeam) chatchannels |= GarblerChannels.PvPTeam;
        if (garblerState.Echo) chatchannels |= GarblerChannels.Echo;
        if (garblerState.Roleplay) chatchannels |= GarblerChannels.Roleplay;

        // Linkshell Permissions
        if (garblerState.Ls1) chatchannels |= GarblerChannels.Ls1;
        if (garblerState.Ls2) chatchannels |= GarblerChannels.Ls2;
        if (garblerState.Ls3) chatchannels |= GarblerChannels.Ls3;
        if (garblerState.Ls4) chatchannels |= GarblerChannels.Ls4;
        if (garblerState.Ls5) chatchannels |= GarblerChannels.Ls5;
        if (garblerState.Ls6) chatchannels |= GarblerChannels.Ls6;
        if (garblerState.Ls7) chatchannels |= GarblerChannels.Ls7;
        if (garblerState.Ls8) chatchannels |= GarblerChannels.Ls8;
        if (garblerState.Cwl1) chatchannels |= GarblerChannels.Cwl1;
        if (garblerState.Cwl2) chatchannels |= GarblerChannels.Cwl2;
        if (garblerState.Cwl3) chatchannels |= GarblerChannels.Cwl3;
        if (garblerState.Cwl4) chatchannels |= GarblerChannels.Cwl4;
        if (garblerState.Cwl5) chatchannels |= GarblerChannels.Cwl5;
        if (garblerState.Cwl6) chatchannels |= GarblerChannels.Cwl6;
        if (garblerState.Cwl7) chatchannels |= GarblerChannels.Cwl7;
        if (garblerState.Cwl8) chatchannels |= GarblerChannels.Cwl8;

        return new UserGarblerSettings(garblerState.GarblerEnabled, garblerState.GarblerLocked, garblerState.GarblerChannelsLocked, chatchannels);
    }

}
