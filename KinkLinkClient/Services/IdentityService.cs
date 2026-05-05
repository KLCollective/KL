using System;
using KinkLinkClient.Domain;

namespace KinkLinkClient.Services;

/// <summary>
///     Manages a flavor aspect of body swapping and twinning
/// </summary>
public class IdentityService
{
    /// <summary>
    ///     Your friend code
    /// </summary>
    public string FriendCode { get; private set; } = string.Empty;

    public event EventHandler<String>? IdentityUpdated;

    /// <summary>
    ///     The current alteration to the local character
    /// </summary>
    public IdentityAlteration? Alteration;

    /// <summary>
    ///     Returns if the local player is being altered in any way
    /// </summary>
    public bool IsAltered => Alteration is not null;

    /// <summary>
    ///     Sets the current friend code
    /// </summary>
    public void SetFriendCode(string code)
    {
        this.FriendCode = code;
        IdentityUpdated?.Invoke(this, this.FriendCode);
        Plugin.CharacterConfiguration.ProfileUID = this.FriendCode;
        Plugin.CharacterConfiguration?.Save().ConfigureAwait(false);
    }

    public void ClearFriendCode()
    {
        FriendCode = string.Empty;
        IdentityUpdated?.Invoke(this, this.FriendCode);
    }

    /// <summary>
    ///     Clears any alterations made to the local player
    /// </summary>
    public void ClearAlterations()
    {
        Alteration = null;
    }

    /// <summary>
    ///     Adds an alteration to the current identity
    /// </summary>
    public void AddAlteration(IdentityAlterationType type, string sender)
    {
        Alteration = new IdentityAlteration(type, sender);
    }
}
