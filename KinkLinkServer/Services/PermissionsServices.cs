using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Security;
using KinkLinkServer.Domain;

namespace KinkLinkServer.Services;

public class PermissionsService
{
    private readonly ILogger<PermissionsService> _logger;
    private readonly PairsService _pairsService;
    private readonly KinkLinkProfilesService _profilesService;

    public PermissionsService(
        Configuration config,
        ILogger<PermissionsService> logger,
        PairsService pairsService,
        KinkLinkProfilesService profilesService
    )
    {
        _logger = logger;
        _pairsService = pairsService;
        _profilesService = profilesService;
    }

    public async Task<DBPairResult> CreatePermissions(string userUID, string targetUID)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userUID) || string.IsNullOrWhiteSpace(targetUID))
            {
                _logger.LogWarning("CreatePairRequest failed: invalid UIDs provided");
                return DBPairResult.PairUIDDoesNotExist;
            }

            if (userUID == targetUID)
            {
                _logger.LogWarning("CreatePairRequest failed: user cannot pair with themselves");
                return DBPairResult.PairUIDDoesNotExist;
            }

            var userProfile = await _profilesService.GetIdFromUidAsync(userUID);
            var targetProfile = await _profilesService.GetIdFromUidAsync(targetUID);

            if (userProfile == null)
            {
                _logger.LogWarning(
                    "CreatePairRequest failed: user UID {UserUID} not found",
                    userUID
                );
                return DBPairResult.PairUIDDoesNotExist;
            }

            if (targetProfile == null)
            {
                _logger.LogWarning(
                    "CreatePairRequest failed: target UID {TargetUID} not found",
                    targetUID
                );
                return DBPairResult.PairUIDDoesNotExist;
            }

            var newPair = await _pairsService.AddPairAsync(userProfile.Value, targetProfile.Value);

            if (newPair != null)
            {
                _logger.LogInformation(
                    "CreatePairRequest: successfully created pair from {UserUID} to {TargetUID}",
                    userUID,
                    targetUID
                );
                return DBPairResult.PairCreated;
            }

            _logger.LogError(
                "CreatePairRequest: failed to create pair from {UserUID} to {TargetUID}",
                userUID,
                targetUID
            );
            return DBPairResult.UnknownError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePairRequest failed with unexpected error");
            return DBPairResult.UnknownError;
        }
    }

    public async Task<DBPairResult> UpdatePermissions(
        string senderFriendCode,
        string targetFriendCode,
        UserPermissions permissions
    )
    {
        try
        {
            if (
                string.IsNullOrWhiteSpace(senderFriendCode)
                || string.IsNullOrWhiteSpace(targetFriendCode)
            )
            {
                _logger.LogWarning("UpdatePermissions failed: invalid friend codes provided");
                return DBPairResult.PairUIDDoesNotExist;
            }

            if (senderFriendCode == targetFriendCode)
            {
                _logger.LogWarning("UpdatePermissions failed: cannot update permissions with self");
                return DBPairResult.PairUIDDoesNotExist;
            }

            var senderProfile = await _profilesService.GetIdFromUidAsync(senderFriendCode);
            var targetProfile = await _profilesService.GetIdFromUidAsync(targetFriendCode);

            if (senderProfile == null)
            {
                _logger.LogWarning(
                    "UpdatePermissions failed: sender UID {SenderUID} not found",
                    senderFriendCode
                );
                return DBPairResult.PairUIDDoesNotExist;
            }

            if (targetProfile == null)
            {
                _logger.LogWarning(
                    "UpdatePermissions failed: target UID {TargetUID} not found",
                    targetFriendCode
                );
                return DBPairResult.PairUIDDoesNotExist;
            }

            var senderId = senderProfile.Value;
            var targetId = targetProfile.Value;

            var updateResult = await _pairsService.UpdatePairPermissionsAsync(
                senderId,
                targetId,
                (int)permissions.Perms
            );

            if (updateResult != null)
            {
                _logger.LogInformation(
                    "UpdatePermissions: successfully updated permissions from {SenderUID} to {TargetUID}",
                    senderFriendCode,
                    targetFriendCode
                );
                return DBPairResult.Success;
            }

            _logger.LogError(
                "UpdatePermissions: failed to update permissions from {SenderUID} to {TargetUID}",
                senderFriendCode,
                targetFriendCode
            );
            return DBPairResult.UnknownError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePermissions failed with unexpected error");
            return DBPairResult.UnknownError;
        }
    }

    public async Task<TwoWayPermissions?> GetPermissions(string userUID, string targetUID)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userUID) || string.IsNullOrWhiteSpace(targetUID))
            {
                _logger.LogWarning("GetPermissions failed: invalid UIDs provided");
                return null;
            }

            if (userUID == targetUID)
            {
                _logger.LogWarning("GetPermissions failed: cannot get permissions with self");
                return null;
            }

            var permissionsGrantedTo = await _pairsService.GetPairByProfileIdsAsync(
                userUID,
                targetUID
            );
            var permissionsGrantedBy = await _pairsService.GetPairByProfileIdsAsync(
                targetUID,
                userUID
            );

            if (permissionsGrantedTo == null || permissionsGrantedBy == null)
            {
                _logger.LogWarning("GetPermissions failed: user UID {UserUID} not found", userUID);
                return null;
            }
            _logger.LogDebug(
                "GetPermissions: retrieved permissions from {UserUID} to {TargetUID}",
                userUID,
                targetUID
            );
            return new TwoWayPermissions(
                userUID,
                targetUID,
                permissionsGrantedTo,
                permissionsGrantedBy
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPermissions failed with unexpected error");
            return null;
        }
    }

    public async Task<DBPairResult> DeletePermissions(string userUID, string targetUID)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userUID) || string.IsNullOrWhiteSpace(targetUID))
            {
                _logger.LogWarning("DeletePermissions failed: invalid UIDs provided");
                return DBPairResult.PairUIDDoesNotExist;
            }

            if (userUID == targetUID)
            {
                _logger.LogWarning("DeletePermissions failed: cannot delete permissions with self");
                return DBPairResult.PairUIDDoesNotExist;
            }

            var userProfile = await _profilesService.GetIdFromUidAsync(userUID);
            var targetProfile = await _profilesService.GetIdFromUidAsync(targetUID);

            if (userProfile == null)
            {
                _logger.LogWarning(
                    "DeletePermissions failed: user UID {UserUID} not found",
                    userUID
                );
                return DBPairResult.PairUIDDoesNotExist;
            }

            if (targetProfile == null)
            {
                _logger.LogWarning(
                    "DeletePermissions failed: target UID {TargetUID} not found",
                    targetUID
                );
                return DBPairResult.PairUIDDoesNotExist;
            }

            var userId = userProfile.Value;
            var targetId = targetProfile.Value;

            var deleteResult = await _pairsService.RemovePairAsync(userId, targetId);

            if (deleteResult)
            {
                _logger.LogInformation(
                    "DeletePermissions: successfully deleted pair from {UserUID} to {TargetUID}",
                    userUID,
                    targetUID
                );
                return DBPairResult.Success;
            }

            _logger.LogError(
                "DeletePermissions: failed to delete pair from {UserUID} to {TargetUID}",
                userUID,
                targetUID
            );
            return DBPairResult.UnknownError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeletePermissions failed with unexpected error");
            return DBPairResult.UnknownError;
        }
    }

    private async Task<bool> IsTwoWayPaired(string user, string pair)
    {
        var userProfile = await _profilesService.GetIdFromUidAsync(user);
        var pairProfile = await _profilesService.GetIdFromUidAsync(pair);
        if (userProfile == null || pairProfile == null)
            return false;

        return await _pairsService.ConfirmTwoWayPairAsync(userProfile.Value, pairProfile.Value);
    }

    public async Task<List<TwoWayPermissions>> GetAllPermissions(string userUID)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userUID))
            {
                _logger.LogWarning("GetAllPermissions failed: invalid UID provided");
                return new List<TwoWayPermissions>();
            }

            _logger.LogDebug(
                "GetAllPermissions: retrieved {Count} permissions for UID {UserUID}",
                0,
                userUID
            );
            return new List<TwoWayPermissions>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllPermissions failed with unexpected error");
            return new List<TwoWayPermissions>();
        }
    }
}
