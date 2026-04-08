using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Login;

public class LoginViewUiController : IDisposable
{
    // Injected
    private readonly NetworkService _networkService;
    private readonly LoginManager _loginManager;

    /// <summary>
    ///     User inputted secret
    /// </summary>
    public string Secret = string.Empty;
    public string SelectedProfileUID = string.Empty;
    public List<(string, string)> AvailableProfileUids = [];
    public bool ProfilesAvailable => AvailableProfileUids.Count > 0;
    public bool IsQuerying { get; private set; }

    public LoginViewUiController(NetworkService networkService, LoginManager loginManager)
    {
        _networkService = networkService;
        _loginManager = loginManager;
        _loginManager.LoginFinished += OnLoginFinished;
        if (Plugin.Configuration is not null)
            Secret = Plugin.Configuration.SecretKey;
        if (Plugin.CharacterConfiguration is not null)
            SelectedProfileUID = Plugin.CharacterConfiguration.ProfileUID;
    }

    public async void GetProfileUids()
    {
        if (IsQuerying)
            return;
        if (string.IsNullOrWhiteSpace(Secret))
            return;

        IsQuerying = true;
        var result = await _networkService.GetProfilesAsync(Secret);
        IsQuerying = false;

        AvailableProfileUids = result;
    }

    public async void Connect()
    {
        try
        {
            // Only save if the configuration is set
            if (Plugin.Configuration is null || Plugin.CharacterConfiguration is null)
                return;

            // Don't save if the string is empty
            if (string.IsNullOrWhiteSpace(Secret) || string.IsNullOrWhiteSpace(SelectedProfileUID))
                return;

            // Set the secret
            Plugin.Configuration.SecretKey = this.Secret;
            Plugin.CharacterConfiguration.ProfileUID = this.SelectedProfileUID;

            // Save the configuration
            await Plugin.Configuration.Save().ConfigureAwait(false);
            await Plugin.CharacterConfiguration.Save().ConfigureAwait(false);
            // Try to connect to the server
            await _networkService.StartAsync();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    // TODO: This needs to redirect to the actual server. Actually, IDK if I will make it public?
    public static void OpenDiscordLink() =>
        NotificationHelper.Info(
            "KinkLink",
            "Our discord community is currently private, please have a friend refer you"
        );

    private void OnLoginFinished()
    {
        if (Plugin.CharacterConfiguration is null)
            return;

        SelectedProfileUID = Plugin.CharacterConfiguration.ProfileUID;
    }

    public void Dispose()
    {
        _loginManager.LoginFinished -= OnLoginFinished;
        GC.SuppressFinalize(this);
    }
}
