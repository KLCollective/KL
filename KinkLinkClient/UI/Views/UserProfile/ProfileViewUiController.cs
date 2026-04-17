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

    public ProfileViewUiController(
        NetworkService networkService,
        ProfileService profileService,
        IdentityService identityService
    )
    {
        _profileService = profileService;
        _identityService = identityService;
        _networkService = networkService;
        _networkService.Connected += LoadProfile;
    }

    private async Task LoadProfile()
    {
        try
        {
            var uid = _identityService.FriendCode;
            if (string.IsNullOrEmpty(uid))
                return;

            var profileResponse = await _networkService.InvokeAsync<ActionResult<KinkLinkProfile>>(
                HubMethod.GetProfile,
                uid
            );

            if (
                profileResponse?.Result == ActionResultEc.Success
                && profileResponse.Value is { } profile
            )
            {
                Alias = profile.Alias ?? string.Empty;
                Title = profile.Title;
                Description = profile.Description ?? string.Empty;
                ChatRole = profile.ChatRole ?? string.Empty;
                _profileService.UpdateProfile(profile);
            }

            var configResponse = await _networkService.InvokeAsync<ActionResult<KinkLinkProfileConfig>>(
                HubMethod.GetProfileConfig
            );

            if (
                configResponse?.Result == ActionResultEc.Success
                && configResponse.Value is { } config
            )
            {
                EnableGlamours = config.EnableGlamours;
                EnableGarbler = config.EnableGarbler;
                EnableGarblerChannels = config.EnableGarblerChannels;
                EnableMoodles = config.EnableMoodles;
                _profileService.UpdateConfig(config);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ProfileViewUiController.LoadProfile] {e}");
        }
    }

    public async void SaveProfile()
    {
        try
        {
            var profileResponse = await _networkService.InvokeAsync<UpdateProfileResponse>(
                HubMethod.UpdateProfile,
                new UpdateProfileRequest(Alias, Title, Description, ChatRole)
            );

            if (
                profileResponse?.Result == ActionResultEc.Success
                && profileResponse.Profile is { } profile
            )
            {
                _profileService.UpdateProfile(profile);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ProfileViewUiController.SaveProfile] {e}");
        }
    }

    public async void SaveConfig()
    {
        try
        {
            var uid = _identityService.FriendCode;
            if (string.IsNullOrEmpty(uid))
                return;

            var configResponse = await _networkService.InvokeAsync<UpdateProfileConfigResponse>(
                HubMethod.UpdateProfileConfig,
                new UpdateProfileConfigRequest(
                    uid,
                    EnableGlamours,
                    EnableGarbler,
                    EnableGarblerChannels,
                    EnableMoodles
                )
            );

            if (
                configResponse?.Result == ActionResultEc.Success
                && configResponse.Config is { } config
            )
            {
                _profileService.UpdateConfig(config);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ProfileViewUiController.SaveConfig] {e}");
        }
    }
}
