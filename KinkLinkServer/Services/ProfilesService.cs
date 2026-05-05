using System.Diagnostics;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class KinkLinkProfilesService
{
    private readonly ILogger<KinkLinkProfilesService> _logger;
    private readonly ProfilesSql _profilesSql;
    private readonly IMetricsService _metricsService;

    public KinkLinkProfilesService(
        Configuration config,
        IMetricsService metricsService,
        ILogger<KinkLinkProfilesService> logger
    )
    {
        _logger = logger;
        _profilesSql = new ProfilesSql(config.DatabaseConnectionString);
        _metricsService = metricsService;
    }

    public async Task<bool> ExistsAsync(string uid)
    {
        _logger.LogTrace("ExistsAsync({Uid})", uid);
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await _profilesSql.ProfileExistsAsync(new(uid));
            success = result is { } row && row.Exists;
            _logger.LogTrace("ExistsAsync({Uid}) -> {Result}", uid, success);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if profile exists for {Uid}", uid);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("ProfileExists", success);
            _metricsService.RecordDatabaseOperationDuration(
                "ProfileExists",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    public async Task<int?> GetIdFromUidAsync(string uid)
    {
        _logger.LogTrace("GetIdFromUidAsync({Uid})", uid);
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var profile = await _profilesSql.GetProfileByUidAsync(new(uid));
            success = profile.HasValue;
            _logger.LogTrace("GetIdFromUidAsync({Uid}) -> {Id}", uid, profile?.UserId);
            return profile?.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile ID for {Uid}", uid);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetIdFromUid", success);
            _metricsService.RecordDatabaseOperationDuration(
                "GetIdFromUid",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    public async Task<KinkLinkProfile?> GetProfileByUidAsync(string uid)
    {
        _logger.LogTrace("GetProfileByUidAsync({Uid})", uid);
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await _profilesSql.GetProfileByUidAsync(new(uid));
            if (result is not { } row)
            {
                _logger.LogWarning("Profile not found for {Uid}", uid);
                return null;
            }

            success = true;
            _logger.LogTrace("Profile found for {Uid}", uid);
            return new KinkLinkProfile(
                row.Uid,
                row.ChatRole ?? string.Empty,
                row.Alias ?? string.Empty,
                Enum.Parse<Title>(row.Title ?? nameof(Title.Kinkster)),
                row.Description ?? string.Empty,
                null,
                null
            );
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error getting profile for {Uid}", uid);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetProfileByUid", success);
            _metricsService.RecordDatabaseOperationDuration(
                "GetProfileByUid",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    public async Task<KinkLinkProfile?> UpdateDetailsByUidAsync(
        string uid,
        KinkLinkCommon.Domain.Network.Title title,
        string alias,
        string chatRole,
        string description
    )
    {
        _logger.LogInformation(
            "UpdateDetailsByUidAsync({Uid}): title={Title}, alias={Alias}",
            uid,
            title,
            alias
        );
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var user_id = await GetIdFromUidAsync(uid);
            if (user_id is not { } id)
            {
                _logger.LogWarning("Profile not found for {Uid}", uid);
                return null;
            }

            var result = await _profilesSql.UpdateDetailsForProfileAsync(
                new(title.ToString(), description, alias, chatRole, uid, id)
            );

            if (result is not { } row)
            {
                _logger.LogError("Failed to update profile for {Uid}", uid);
                return null;
            }

            success = true;
            _logger.LogInformation("Profile updated for {Uid}", uid);
            return new KinkLinkProfile(
                row.Uid,
                row.ChatRole,
                row.Alias,
                Enum.Parse<Title>(row.Title ?? nameof(Title.Kinkster)),
                row.Description,
                row.CreatedAt,
                row.UpdatedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile details for {Uid}", uid);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("UpdateDetailsByUid", success);
            _metricsService.RecordDatabaseOperationDuration(
                "UpdateDetailsByUid",
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}
