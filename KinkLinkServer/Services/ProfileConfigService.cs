using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkServer.Domain;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class KinkLinkProfileConfigService
{
    private readonly ILogger<KinkLinkProfileConfigService> _logger;
    private readonly ProfileConfigSql _profileConfigSql;
    private readonly ProfilesSql _profilesSql;

    public KinkLinkProfileConfigService(
        Configuration config,
        ILogger<KinkLinkProfileConfigService> logger
    )
    {
        _logger = logger;
        _profileConfigSql = new ProfileConfigSql(config.DatabaseConnectionString);
        _profilesSql = new ProfilesSql(config.DatabaseConnectionString);
    }

    private async Task<int?> GetProfileIdFromUidAsync(string uid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(uid));
        return profile?.Id;
    }

    public async Task<KinkLinkProfileConfig?> GetProfileConfigByUidAsync(string uid)
    {
        _logger.LogDebug("GetProfileConfigByUidAsync({Uid})", uid);
        try
        {
            var result = await _profileConfigSql.GetProfileConfigByUidAsync(new(uid));
            if (result is not { } row)
            {
                _logger.LogWarning("Profile config not found for {Uid}", uid);
                return null;
            }

            _logger.LogDebug("Profile config found for {Uid}", uid);
            return new KinkLinkProfileConfig(
                row.EnableGlamours ?? false,
                row.EnableGarbler ?? false,
                row.EnableGarblerChannels ?? false,
                row.EnableMoodles ?? false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile config for {Uid}", uid);
            throw;
        }
    }

    public async Task<KinkLinkProfileConfig?> UpdateProfileConfigAsync(
        string uid,
        bool enableGlamours,
        bool enableGarbler,
        bool enableGarblerChannels,
        bool enableMoodles
    )
    {
        _logger.LogInformation(
            "UpdateProfileConfigAsync({Uid}): glamours={G}, garbler={Gbr}, channels={Ch}, moodles={M}",
            uid,
            enableGlamours,
            enableGarbler,
            enableGarblerChannels,
            enableMoodles
        );

        try
        {
            var profileId = await GetProfileIdFromUidAsync(uid);
            if (profileId is not { } id)
            {
                _logger.LogWarning("Profile not found for {Uid}", uid);
                return null;
            }

            var result = await _profileConfigSql.CreateOrUpdateProfileConfigAsync(
                new(id, enableGlamours, enableGarbler, enableGarblerChannels, enableMoodles)
            );

            if (result is not { } row)
            {
                _logger.LogError("Failed to update profile config for {Uid}", uid);
                return null;
            }

            _logger.LogInformation("Profile config updated for {Uid}", uid);
            return new KinkLinkProfileConfig(
                row.EnableGlamours ?? false,
                row.EnableGarbler ?? false,
                row.EnableGarblerChannels ?? false,
                row.EnableMoodles ?? false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile config for {Uid}", uid);
            throw;
        }
    }

    public async Task<KinkLinkProfileConfig?> DeleteProfileConfigByUidAsync(string uid)
    {
        _logger.LogInformation("DeleteProfileConfigByUidAsync({Uid})", uid);

        try
        {
            var profileId = await GetProfileIdFromUidAsync(uid);
            if (profileId is not { } id)
            {
                _logger.LogWarning("Profile not found for {Uid}", uid);
                return null;
            }

            var result = await _profileConfigSql.DeleteProfileConfigAsync(new(id));

            if (result is not { } row)
            {
                _logger.LogError("Failed to delete profile config for {Uid}", uid);
                return null;
            }

            _logger.LogInformation("Profile config deleted for {Uid}", uid);
            return new KinkLinkProfileConfig(
                row.EnableGlamours ?? false,
                row.EnableGarbler ?? false,
                row.EnableGarblerChannels ?? false,
                row.EnableMoodles ?? false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile config for {Uid}", uid);
            throw;
        }
    }
}
