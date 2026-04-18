using System.Diagnostics;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;

namespace KinkLinkServer.Services;

public class KinkLinkProfilesService
{
    private readonly ProfilesSql _profilesSql;
    private readonly IMetricsService _metricsService;

    public KinkLinkProfilesService(Configuration config, IMetricsService metricsService)
    {
        _profilesSql = new ProfilesSql(config.DatabaseConnectionString);
        _metricsService = metricsService;
    }

    public async Task<bool> ExistsAsync(string uid)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await _profilesSql.ProfileExistsAsync(new(uid));
            success = result is { } row && row.Exists;
            return success;
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
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var profile = await _profilesSql.GetProfileByUidAsync(new(uid));
            success = profile.HasValue;
            return profile?.Id;
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
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await _profilesSql.GetProfileByUidAsync(new(uid));
            if (result is not { } row)
                return null;

            success = true;
            return new KinkLinkProfile(
                row.Uid,
                row.ChatRole,
                row.Alias,
                Enum.Parse<Title>(row.Title),
                row.Description,
                null,
                null
            );
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
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var profileId = await GetIdFromUidAsync(uid);
            if (profileId is not { } id)
                return null;

            var result = await _profilesSql.UpdateDetailsForProfileAsync(
                new(title.ToString(), description, uid, id)
            );

            if (result is not { } row)
                return null;

            success = true;
            return new KinkLinkProfile(
                row.Uid,
                row.ChatRole,
                row.Alias,
                Enum.Parse<Title>(row.Title),
                row.Description,
                row.CreatedAt,
                row.UpdatedAt
            );
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

