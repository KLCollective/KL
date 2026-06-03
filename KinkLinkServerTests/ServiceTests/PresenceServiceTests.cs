using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

public class PresenceServiceTests
{
    private readonly IPresenceService _presenceService;
    private readonly ILogger<PresenceService> _logger;

    public PresenceServiceTests()
    {
        _logger = LoggerFactory.Create(builder => { }).CreateLogger<PresenceService>();
        _presenceService = new PresenceService(_logger);
    }

    [Fact]
    public void TryGet_UserNotAdded_ReturnsNull()
    {
        var result = _presenceService.TryGet("USER1");

        Assert.Null(result);
    }

    [Fact]
    public void Add_UserAdded_ReturnsPresence()
    {
        var presence = new Presence("conn-1", "CharacterName", "World");
        _presenceService.Add("USER1", presence);

        var result = _presenceService.TryGet("USER1");

        Assert.NotNull(result);
        Assert.Equal("conn-1", result.ConnectionId);
        Assert.Equal("CharacterName", result.CharacterName);
        Assert.Equal("World", result.CharacterWorld);
    }

    [Fact]
    public void Add_DuplicateKey_DoesNotOverwrite()
    {
        var presence1 = new Presence("conn-1", "Char1", "World1");
        var presence2 = new Presence("conn-2", "Char2", "World2");

        _presenceService.Add("USER1", presence1);
        _presenceService.Add("USER1", presence2); // TryAdd won't overwrite

        var result = _presenceService.TryGet("USER1");

        Assert.NotNull(result);
        // The first presence should remain (ConcurrentDictionary.TryAdd doesn't overwrite)
        Assert.Equal("conn-1", result.ConnectionId);
    }

    [Fact]
    public void Remove_UserRemoved_ReturnsNull()
    {
        var presence = new Presence("conn-1", "Char", "World");
        _presenceService.Add("USER1", presence);
        _presenceService.Remove("USER1");

        var result = _presenceService.TryGet("USER1");
        Assert.Null(result);
    }

    [Fact]
    public void Remove_NonExistentUser_DoesNotThrow()
    {
        var exception = Record.Exception(() => _presenceService.Remove("NONEXISTENT"));

        Assert.Null(exception);
    }

    [Fact]
    public void IsUserExceedingCooldown_UserNotFound_ReturnsTrue()
    {
        var result = _presenceService.IsUserExceedingCooldown("NONEXISTENT");

        Assert.True(result);
    }

    [Fact]
    public void IsUserExceedingCooldown_FirstCall_ReturnsFalse()
    {
        var presence = new Presence("conn-1", "Char", "World");
        _presenceService.Add("USER1", presence);

        var result = _presenceService.IsUserExceedingCooldown("USER1");

        Assert.False(result);
    }

    [Fact]
    public void GetOnlineCount_NoUsers_ReturnsZero()
    {
        var result = _presenceService.GetOnlineCount();

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetOnlineCount_MultipleUsers_ReturnsCorrectCount()
    {
        _presenceService.Add("USER1", new Presence("c1", "Char1", "World1"));
        _presenceService.Add("USER2", new Presence("c2", "Char2", "World2"));
        _presenceService.Add("USER3", new Presence("c3", "Char3", "World3"));

        var result = _presenceService.GetOnlineCount();

        Assert.Equal(3, result);
    }

    [Fact]
    public void GetOnlineCount_AfterRemove_CountDecrements()
    {
        _presenceService.Add("USER1", new Presence("c1", "Char1", "World1"));
        _presenceService.Add("USER2", new Presence("c2", "Char2", "World2"));
        _presenceService.Remove("USER1");

        var result = _presenceService.GetOnlineCount();

        Assert.Equal(1, result);
    }

    [Fact]
    public void Add_UpdatesPresenceLast_ToMinValue()
    {
        var presence = new Presence("conn-1", "Char", "World");

        Assert.Equal(DateTime.MinValue, presence.Last);
    }
}
