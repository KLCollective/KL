using System;
using System.Threading.Tasks;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Profile;
using KinkLinkCommon.Domain.Network.ProfileConfig;

namespace KinkLinkClient.UI.Views.UserProfile;

public class ProfileViewUiController
{
    private NetworkService _networkService;
    private ProfileService _profileService;
    private IdentityService _identityService;

    public string Alias = string.Empty;
    public Title Title = KinkLinkCommon.Domain.Network.Title.Kinkster;
    public string Description = string.Empty;

    public string ChatRole = string.Empty;

    public bool EnableGlamours;
    public bool EnableGarbler;
    public bool EnableGarblerChannels;
    public bool EnableMoodles;

    public ProfileViewUiController(ProfileService profileService, IdentityService identityService)
    {
        _profileService = profileService;
        _identityService = identityService;

        _profileService.ProfileUpdated += UpdateProfile;
        _profileService.ConfigUpdated += UpdateConfig;
    }

    private void UpdateConfig(object? sender, KinkLinkProfileConfig profile)
    {
        EnableGlamours = profile.EnableGlamours;
        EnableGarbler = profile.EnableGarbler;
        EnableGarblerChannels = profile.EnableGarblerChannels;
        EnableMoodles = profile.EnableMoodles;
    }

    private void UpdateProfile(object? sender, KinkLinkProfile profile)
    {
        Alias = profile.Alias ?? string.Empty;
        Title = profile.Title;
        Description = profile.Description ?? string.Empty;
        ChatRole = profile.ChatRole ?? string.Empty;
    }

    public async void SaveProfile()
    {
        await _profileService.UpdateProfile(Alias, Title, Description, ChatRole);
    }

    public async void SaveConfig()
    {
        await _profileService.UpdateConfig(
            EnableGlamours,
            EnableGarbler,
            EnableGarblerChannels,
            EnableMoodles
        );
    }
}
