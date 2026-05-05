using System;
using System.Threading.Tasks;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Profile;
using KinkLinkCommon.Domain.Network.ProfileConfig;

namespace KinkLinkClient.Services;

public class ProfileService
{
    public KinkLinkProfile? CurrentProfile { get; private set; }
    public KinkLinkProfileConfig? CurrentConfig { get; private set; }
    public IdentityService _identityService;
    public NetworkService _networkService;

    public event EventHandler<KinkLinkProfile>? ProfileUpdated;
    public event EventHandler<KinkLinkProfileConfig>? ConfigUpdated;

    public ProfileService(IdentityService identityService, NetworkService networkService)
    {
        _identityService = identityService;
        _networkService = networkService;

        _identityService.IdentityUpdated += LoadProfile;
    }

    private async void LoadProfile(object? _, string uid)
    {
        try
        {
            Plugin.Log.Info(
                $"[ProfileService.LoadProfile] Starting profile load sequence with {uid}"
            );

            if (string.IsNullOrEmpty(uid))
            {
                Plugin.Log.Info("[ProfileService.LoadProfile] UID empty, aborting");
                return;
            }

            Plugin.Log.Info(
                $"[ProfileService.LoadProfile] Invoking {HubMethod.GetProfile} for {uid}"
            );
            var profileResponse = await _networkService.InvokeAsync<ActionResult<KinkLinkProfile>>(
                HubMethod.GetProfile,
                uid
            );
            Plugin.Log.Info(
                $"[ProfileService.LoadProfile] Profile response: {profileResponse?.Result}"
            );

            if (
                profileResponse?.Result == ActionResultEc.Success
                && profileResponse.Value is { } profile
            )
            {
                CurrentProfile = profile;
                Plugin.Log.Info($"[ProfileService.LoadProfile] Profile loaded: {profile.Alias}");
                ProfileUpdated?.Invoke(this, profile);
                Plugin.Log.Info("[ProfileService.LoadProfile] ProfileUpdated event invoked");
            }
            else
            {
                Plugin.Log.Info($"[ProfileService.LoadProfile] Profile load failed or empty");
            }

            Plugin.Log.Info($"[ProfileService.LoadProfile] Invoking {HubMethod.GetProfileConfig}");
            var configResponse = await _networkService.InvokeAsync<
                ActionResult<KinkLinkProfileConfig>
            >(HubMethod.GetProfileConfig);
            Plugin.Log.Info(
                $"[ProfileService.LoadProfile] Config response: {configResponse?.Result}"
            );

            if (
                configResponse?.Result == ActionResultEc.Success
                && configResponse.Value is { } config
            )
            {
                CurrentConfig = config;
                Plugin.Log.Info(
                    $"[ProfileService.LoadProfile] Config loaded: Glamours={config.EnableGlamours}, Garbler={config.EnableGarbler}"
                );
                ConfigUpdated?.Invoke(this, config);
                Plugin.Log.Info("[ProfileService.LoadProfile] ConfigUpdated event invoked");
            }
            else
            {
                Plugin.Log.Info($"[ProfileService.LoadProfile] Config load failed or empty");
            }

            Plugin.Log.Info("[ProfileService.LoadProfile] Profile load sequence complete");
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ProfileService.LoadProfile] {e}");
        }
    }

    public async Task UpdateProfile(
        String profileAlias,
        Title profileTitle,
        String profileDescription,
        String profileChatRole
    )
    {
        try
        {
            Plugin.Log.Info(
                $"[ProfileService.UpdateProfile] Starting update: Alias={profileAlias}, Title={profileTitle}"
            );

            var profileResponse = await _networkService.InvokeAsync<UpdateProfileResponse>(
                HubMethod.UpdateProfile,
                new UpdateProfileRequest(
                    profileAlias,
                    profileTitle,
                    profileDescription,
                    profileChatRole
                )
            );
            Plugin.Log.Info($"[ProfileService.UpdateProfile] Response: {profileResponse?.Result}");

            if (
                profileResponse?.Result == ActionResultEc.Success
                && profileResponse.Profile is { } profile
            )
            {
                CurrentProfile = profile;
                Plugin.Log.Info($"[ProfileService.UpdateProfile] Profile updated: {profile.Alias}");
                ProfileUpdated?.Invoke(this, profile);
                Plugin.Log.Info("[ProfileService.UpdateProfile] ProfileUpdated event invoked");
            }
            else
            {
                Plugin.Log.Info("[ProfileService.UpdateProfile] Update failed or empty response");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ProfileService.UpdateProfile] {e}");
        }
    }

    public async Task UpdateConfig(
        bool EnableGlamours,
        bool EnableGarbler,
        bool EnableGarblerChannels,
        bool EnableMoodles
    )
    {
        try
        {
            Plugin.Log.Info(
                $"[ProfileService.UpdateConfig] Starting update: Glamours={EnableGlamours}, Garbler={EnableGarbler}, Channels={EnableGarblerChannels}, Moodles={EnableMoodles}"
            );

            var uid = _identityService.FriendCode;
            Plugin.Log.Info($"[ProfileService.UpdateConfig] UID: {uid}");

            if (string.IsNullOrEmpty(uid))
            {
                Plugin.Log.Info("[ProfileService.UpdateConfig] UID empty, aborting");
                return;
            }

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
            Plugin.Log.Info($"[ProfileService.UpdateConfig] Response: {configResponse?.Result}");

            if (
                configResponse?.Result == ActionResultEc.Success
                && configResponse.Config is { } config
            )
            {
                CurrentConfig = config;
                Plugin.Log.Info(
                    $"[ProfileService.UpdateConfig] Config updated: Glamours={config.EnableGlamours}, Garbler={config.EnableGarbler}"
                );
                ConfigUpdated?.Invoke(this, config);
                Plugin.Log.Info("[ProfileService.UpdateConfig] ConfigUpdated event invoked");
            }
            else
            {
                Plugin.Log.Info("[ProfileService.UpdateConfig] Update failed or empty response");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ProfileService.UpdateConfig] {e}");
        }
    }

    public void Clear()
    {
        CurrentProfile = null;
        CurrentConfig = null;
    }
}
