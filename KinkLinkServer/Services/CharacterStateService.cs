using System.Collections.Concurrent;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Util;

namespace KinkLinkServer.Services;

public class CharacterStateService
{
    private readonly ConcurrentDictionary<string, CharacterStateDto> _cachedStates = [];
    private readonly ILogger<CharacterStateService> _logger;

    public CharacterStateService(ILogger<CharacterStateService> logger)
    {
        _logger = logger;
    }

    public void UpdateState(string friendCode, CharacterStateDto state)
    {
        _cachedStates[friendCode] = state;
        _logger.LogDebug("Updated state cache for {FriendCode}", friendCode);
    }

    public CharacterStateDto? GetState(string friendCode)
    {
        return _cachedStates.TryGetValue(friendCode, out var state) ? state : null;
    }

    public void RemoveState(string friendCode)
    {
        _cachedStates.TryRemove(friendCode, out _);
        _logger.LogDebug("Removed state cache for {FriendCode}", friendCode);
    }

    public bool HasGagPermission(UserPermissions permissions) =>
        permissions.Perms.HasFlag(InteractionPerms.CanApplyGag);

    public bool HasGarblerPermission(UserPermissions permissions) =>
        permissions.Perms.HasFlag(InteractionPerms.CanEnableGarbler);

    public bool HasWardrobePermission(UserPermissions permissions) =>
        permissions.Perms.HasFlag(InteractionPerms.CanApplyWardrobe);

    public bool HasMoodlePermission(UserPermissions permissions) =>
        permissions.Perms.HasFlag(InteractionPerms.CanApplyOwnMoodles) ||
        permissions.Perms.HasFlag(InteractionPerms.CanApplyPairsMoodles);

    public bool CanPerformAction(UserPermissions permissions, KinkLinkCommon.Domain.Enums.Permissions.PairAction action)
    {
        var requiredPerm = action.ToInteractionPerm();
        return permissions.Perms.HasFlag(requiredPerm);
    }
}
