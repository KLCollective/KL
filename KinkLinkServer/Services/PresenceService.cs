using System.Collections.Concurrent;
using KinkLinkCommon;
using KinkLinkServer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class PresenceService(ILogger<PresenceService> logger) : IPresenceService
{
    private readonly ILogger<PresenceService> _logger = logger;
    private readonly ConcurrentDictionary<string, Presence> _presences = [];

    public Presence? TryGet(string friendCode)
    {
        var result = _presences.TryGetValue(friendCode, out var presence) ? presence : null;
        _logger.LogTrace("TryGet({FC}) -> {Result}", friendCode, result is not null);
        return result;
    }

    public void Add(string friendCode, Presence presence)
    {
        _logger.LogTrace("Add({FC]): {Character}@${World}, conn: {Conn}", friendCode, presence.CharacterName, presence.CharacterWorld, presence.ConnectionId);
        _presences.TryAdd(friendCode, presence);
    }

    public void Remove(string friendCode)
    {
        _logger.LogTrace("Remove({FC})", friendCode);
        _presences.TryRemove(friendCode, out _);
    }

    public bool IsUserExceedingCooldown(string friendCode)
    {
        if (_presences.TryGetValue(friendCode, out var presence) is false)
        {
            _logger.LogTrace("IsUserExceedingCooldown({FC}) -> true (not found)", friendCode);
            return true;
        }

        var result = (DateTime.UtcNow - presence.Last).TotalSeconds < Constraints.GlobalCommandCooldownInSeconds;
        _logger.LogTrace("IsUserExceedingCooldown({FC}) -> {Result}", friendCode, result);
        return result;
    }

    public int GetOnlineCount()
    {
        var count = _presences.Count;
        _logger.LogTrace("GetOnlineCount() -> {Count}", count);
        return count;
    }
}

public class Presence(string connectionId, string characterName, string characterWorld)
{
    public string ConnectionId = connectionId;
    public string CharacterName = characterName;
    public string CharacterWorld = characterWorld;
    public DateTime Last = DateTime.MinValue;
}
