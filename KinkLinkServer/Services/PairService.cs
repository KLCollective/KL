using System.Diagnostics;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkServer.Domain;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class PairsService
{
    private readonly ILogger<PairsService> _logger;
    private readonly PairsSql _pairsSql;
    private readonly ProfilesSql _profilesSql;
    private readonly IMetricsService _metricsService;

    public PairsService(Configuration config, ILogger<PairsService> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _pairsSql = new PairsSql(config.DatabaseConnectionString);
        _profilesSql = new ProfilesSql(config.DatabaseConnectionString);
        _metricsService = metricsService;
    }

    private async Task<int?> GetProfileIdFromUidAsync(string uid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(uid));
        return profile?.Id;
    }

    public async Task<string?> GetProfileUidByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var profile = await _pairsSql.GetProfileUidByIdAsync(new(id));
            success = profile.HasValue;
            return profile?.Uid;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetProfileUidById", success);
            _metricsService.RecordDatabaseOperationDuration("GetProfileUidById", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<List<(int, int)>> GetAllPairsForProfileAsync(string uid)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("GetAllPairsForProfileAsync called with uid: {Uid}", uid);
            var result = await _pairsSql.GetAllPairsForProfileAsync(new(uid));
            _logger.LogDebug(
                "GetAllPairsForProfileAsync returned {Count} pairs for uid: {Uid}",
                result.Count,
                uid
            );
            return result.Select(row => (row.Id, row.PairId)).ToList();
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetAllPairsForProfile", true);
            _metricsService.RecordDatabaseOperationDuration("GetAllPairsForProfile", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<UserPermissions?> GetPairByProfileIdsAsync(int id, int pairId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogDebug(
                "GetPairByProfileIdsAsync called with uid: {Id}, pairUid: {PairId}",
                id,
                pairId
            );

            var result = await _pairsSql.GetPairByProfileIdsAsync(new(id, pairId));
            if (result is not { } row)
            {
                _logger.LogWarning(
                    "GetPairByProfileIdsAsync: pair not found for uid: {Id} and pairUid: {PairId}",
                    id,
                    pairId
                );
                return null;
            }

            _logger.LogDebug(
                "GetPairByProfileIdsAsync: found pair for uid: {Id} and pairUid: {PairId}",
                id,
                pairId
            );
            success = true;
            return new UserPermissions(
                row.Id,
                row.PairId,
                row.Expires,
                (RelationshipPriority)(row.Priority ?? 0),
                row.ControlsPerm ?? false,
                row.ControlsConfig ?? false,
                row.DisableSafeword ?? false,
                (int?)row.Interactions
            );
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetPairByProfileIds", success);
            _metricsService.RecordDatabaseOperationDuration("GetPairByProfileIds", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<UserPermissions?> GetPairByProfileIdsAsync(string uid, string pairUid)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogDebug(
                "GetPairByProfileIdsAsync called with uid: {Uid}, pairUid: {PairUid}",
                uid,
                pairUid
            );
            var id = await GetProfileIdFromUidAsync(uid);
            var pairId = await GetProfileIdFromUidAsync(pairUid);

            if (id is null || pairId is null)
            {
                _logger.LogWarning(
                    "GetPairByProfileIdsAsync: profile not found for uid: {Uid} or pairUid: {PairUid}",
                    uid,
                    pairUid
                );
                return null;
            }

            var result = await GetPairByProfileIdsAsync(id.Value, pairId.Value);
            if (result is not { } row)
            {
                _logger.LogWarning(
                    "GetPairByProfileIdsAsync: pair not found for uid: {Uid} and pairUid: {PairUid}",
                    uid,
                    pairUid
                );
                return null;
            }

            _logger.LogDebug(
                "GetPairByProfileIdsAsync: found pair for uid: {Uid} and pairUid: {PairUid}",
                uid,
                pairUid
            );
            return result;
        }
        finally
        {
            stopwatch.Stop();
            success = true;
            _metricsService.IncrementDatabaseOperation("GetPairByProfileIds", success);
            _metricsService.RecordDatabaseOperationDuration("GetPairByProfileIds", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<UserPermissions?> AddPairAsync(int id, int pairId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation("AddPairAsync called with id: {Id}, pairId: {PairId}", id, pairId);
            var result = await _pairsSql.AddPairAsync(new(id, pairId));
            if (result is not { } row)
            {
                _logger.LogError(
                    "AddPairAsync: failed to add pair for id: {Id}, pairId: {PairId}",
                    id,
                    pairId
                );
                return null;
            }

            _logger.LogInformation(
                "AddPairAsync: successfully added pair for id: {Id}, pairId: {PairId}",
                id,
                pairId
            );
            success = true;
            return new UserPermissions(
                row.Id,
                row.PairId,
                row.Expires,
                (RelationshipPriority)(row.Priority ?? 0),
                row.ControlsPerm ?? false,
                row.ControlsConfig ?? false,
                row.DisableSafeword ?? false,
                (int?)row.Interactions
            );
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("AddPair", success);
            _metricsService.RecordDatabaseOperationDuration("AddPair", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<UserPermissions?> AddTemporaryPairAsync(int id, int pairId, DateTime? expires)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation(
                "AddTemporaryPairAsync called with id: {Id}, pairId: {PairId}, expires: {Expires}",
                id,
                pairId,
                expires
            );
            var result = await _pairsSql.AddTemporaryPairAsync(new(id, pairId, expires));
            if (result is not { } row)
            {
                _logger.LogError(
                    "AddTemporaryPairAsync: failed to add temporary pair for id: {Id}, pairId: {PairId}",
                    id,
                    pairId
                );
                return null;
            }

            _logger.LogInformation(
                "AddTemporaryPairAsync: successfully added temporary pair for id: {Id}, pairId: {PairId}, expires: {Expires}",
                id,
                pairId,
                expires
            );
            success = true;
            return new UserPermissions(
                row.Id,
                row.PairId,
                row.Expires,
                (RelationshipPriority)(row.Priority ?? 0),
                row.ControlsPerm ?? false,
                row.ControlsConfig ?? false,
                row.DisableSafeword ?? false,
                (int?)row.Interactions
            );
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("AddTemporaryPair", success);
            _metricsService.RecordDatabaseOperationDuration("AddTemporaryPair", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<bool> RemovePairAsync(int id, int pairId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation(
                "RemovePairAsync called with id: {Id}, pairId: {PairId}",
                id,
                pairId
            );
            var result = await _pairsSql.RemovePairAsync(new(id, pairId));
            success = result != null;
            if (!success)
            {
                _logger.LogWarning(
                    "RemovePairAsync: pair not found for removal id: {Id}, pairId: {PairId}",
                    id,
                    pairId
                );
                return false;
            }

            _logger.LogInformation(
                "RemovePairAsync: successfully removed pair for id: {Id}, pairId: {PairId}",
                id,
                pairId
            );
            return true;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("RemovePair", success);
            _metricsService.RecordDatabaseOperationDuration("RemovePair", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<UserPermissions?> UpdatePairPermissionsAsync(
        int uid,
        int pairUid,
        int interactions
    )
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation(
                "UpdatePairPermissionsAsync called with uid: {Uid}, pairUid: {PairUid}, interactions: {Interactions}",
                uid,
                pairUid,
                interactions
            );
            var result = await _pairsSql.UpdatePairPermissionsAsync(new(interactions, uid, pairUid));
            if (result is not { } row)
            {
                _logger.LogError(
                    "UpdatePairPermissionsAsync: failed to update permissions for uid: {Uid}, pairUid: {PairUid}",
                    uid,
                    pairUid
                );
                return null;
            }

            _logger.LogInformation(
                "UpdatePairPermissionsAsync: successfully updated permissions for uid: {Uid}, pairUid: {PairUid}",
                uid,
                pairUid
            );

            success = true;
            return new UserPermissions(
                uid,
                pairUid,
                null,
                (RelationshipPriority)0,
                false,
                false,
                false,
                interactions
            );
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("UpdatePairPermissions", success);
            _metricsService.RecordDatabaseOperationDuration("UpdatePairPermissions", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<UserPermissions?> UpdatePairControlPermissionsAsync(
        int uid,
        int pairUid,
        bool controlsPerm,
        bool controlsConfig,
        bool disableSafeword
    )
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation(
                "UpdatePairControlPermissionsAsync called with uid: {Uid}, pairUid: {PairUid}, controlsPerm: {ControlsPerm}, controlsConfig: {ControlsConfig}, disableSafeword: {DisableSafeword}",
                uid,
                pairUid,
                controlsPerm,
                controlsConfig,
                disableSafeword
            );
            var result = await _pairsSql.UpdatePairControlPermissionsAsync(
                new(controlsPerm, controlsConfig, disableSafeword, uid, pairUid)
            );
            success = result != null;
            if (!success)
            {
                _logger.LogError(
                    "UpdatePairControlPermissionsAsync: failed to update control permissions for uid: {Uid}, pairUid: {PairUid}",
                    uid,
                    pairUid
                );
                return null;
            }

            _logger.LogInformation(
                "UpdatePairControlPermissionsAsync: successfully updated control permissions for uid: {Uid}, pairUid: {PairUid}",
                uid,
                pairUid
            );
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("UpdatePairControlPermissions", success);
            _metricsService.RecordDatabaseOperationDuration("UpdatePairControlPermissions", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<(bool, bool)> GetPairState(int id, int pairId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogDebug(
                "ConfirmTwoWayPairAsync called with id: {Id}, pairId: {PairId}",
                id,
                pairId
            );
            var result = await _pairsSql.GetPairStateAsync(new(id, pairId));
            var (AtoB, BtoA) = result is null ? (false, false) : (result.Value.Atob, result.Value.Btoa);
            _logger.LogDebug(
                "ConfirmTwoWayPairAsync: id: {Id}, pairId: {PairId}, AtoB: {AtoB}, BtoA: {BtoA}",
                id,
                pairId,
                AtoB,
                BtoA
            );
            success = true;
            return (AtoB, BtoA);
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetPairState", success);
            _metricsService.RecordDatabaseOperationDuration("GetPairState", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<bool> ConfirmTwoWayPairAsync(int id, int pairId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogDebug(
                "ConfirmTwoWayPairAsync called with id: {Id}, pairId: {PairId}",
                id,
                pairId
            );
            var result = await _pairsSql.ConfirmTwoWayPairAsync(new(id, pairId));
            var isTwoWay = result?.Twowaypair ?? false;
            _logger.LogDebug(
                "ConfirmTwoWayPairAsync: id: {Id}, pairId: {PairId}, isTwoWay: {IsTwoWay}",
                id,
                pairId,
                isTwoWay
            );
            success = true;
            return isTwoWay;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("ConfirmTwoWayPair", success);
            _metricsService.RecordDatabaseOperationDuration("ConfirmTwoWayPair", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<int> PurgeExpiredPairsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("PurgeExpiredPairsAsync called");
            var result = await _pairsSql.PurgeExpiredPairsAsync();
            var purged = result != null ? 1 : 0;
            _logger.LogInformation("PurgeExpiredPairsAsync: purged {Count} expired pairs", purged);
            return purged;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("PurgeExpiredPairs", true);
            _metricsService.RecordDatabaseOperationDuration("PurgeExpiredPairs", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<bool> HasExpiredPairsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("HasExpiredPairsAsync called");
            var result = await _pairsSql.HasExpiredPairsAsync();
            var hasExpired = result?.HasExpired ?? false;
            _logger.LogDebug("HasExpiredPairsAsync: hasExpired: {HasExpired}", hasExpired);
            return hasExpired;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("HasExpiredPairs", true);
            _metricsService.RecordDatabaseOperationDuration("HasExpiredPairs", stopwatch.ElapsedMilliseconds);
        }
    }
}