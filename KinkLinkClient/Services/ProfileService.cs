using System;
using KinkLinkCommon.Domain;

namespace KinkLinkClient.Services;

public class ProfileService
{
    public KinkLinkProfile? CurrentProfile { get; private set; }
    public KinkLinkProfileConfig? CurrentConfig { get; private set; }

    public event EventHandler<KinkLinkProfile>? ProfileUpdated;
    public event EventHandler<KinkLinkProfileConfig>? ConfigUpdated;

    public void UpdateProfile(KinkLinkProfile profile)
    {
        CurrentProfile = profile;
        ProfileUpdated?.Invoke(this, profile);
    }

    public void UpdateConfig(KinkLinkProfileConfig config)
    {
        CurrentConfig = config;
        ConfigUpdated?.Invoke(this, config);
    }

    public void Clear()
    {
        CurrentProfile = null;
        CurrentConfig = null;
    }
}
