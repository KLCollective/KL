using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Login;

public static class ServerOptions
{
    public static readonly string[] Names = ["Please select a server", "staging", "local dev"];
    public static readonly string[] Urls =
    [
        "http://localhost",
        "https://kl.apparently-typ.ing",
        "http://localhost:5006",
    ];
}

public class LoginViewUiController : IDisposable
{
    // Injected
    private readonly NetworkService _networkService;
    private readonly LoginManager _loginManager;

    /// <summary>
    ///     User inputted secret
    /// </summary>
    public string Secret = string.Empty;
    public int ServerIndex;
    public string SelectedProfileUID = string.Empty;
    public List<(string, string)> AvailableProfileUids = [("None Selected", "")];
    public bool ProfilesAvailable => AvailableProfileUids.Count > 1;
    public bool CanConnect =>
        ServerIndex != 0
        && !string.IsNullOrEmpty(Secret)
        && !string.IsNullOrEmpty(SelectedProfileUID);
    public bool IsQuerying { get; private set; }

    public LoginViewUiController(NetworkService networkService, LoginManager loginManager)
    {
        _networkService = networkService;
        _loginManager = loginManager;
        _loginManager.LoginFinished += OnLoginFinished;
        if (Plugin.Configuration is not null)
        {
            Secret = Plugin.Configuration.SecretKey;
            ServerIndex = ServerOptions
                .Urls.Select((url, index) => (url, index))
                .FirstOrDefault(x => x.url == Plugin.Configuration.ServerBaseUrl)
                .index;
            _networkService.SetBaseURL(ServerOptions.Urls[ServerIndex]);
            Plugin.Configuration.ServerBaseUrl = ServerOptions.Urls[ServerIndex];
        }
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
        this.AvailableProfileUids = new List<(string, string)> { ("None Selected", "") };
        Plugin.Configuration.SecretKey = this.Secret;
        var result = await _networkService.GetProfilesAsync(Secret);
        await Plugin.Configuration.Save().ConfigureAwait(false);
        IsQuerying = false;

        this.AvailableProfileUids = this.AvailableProfileUids.Concat(result).ToList();
        // We need to prune out any unselected numbers
        if (Plugin.CharacterConfiguration is { } config)
        {
            var index = this.AvailableProfileUids.FindIndex(Profile =>
                Profile.Item1 == config.ProfileUID
            );
            if (index >= 0)
            {
                SelectedProfileUID = this.AvailableProfileUids[index].Item1;
            }
        }
        else
        {
            SelectedProfileUID = "";
        }
    }

    public async void SelectServer(int selectedIndex)
    {
        try
        {
            ServerIndex = selectedIndex;
            Plugin.Configuration.ServerBaseUrl = ServerOptions.Urls[selectedIndex];
            await Plugin.Configuration.Save().ConfigureAwait(false);
            Plugin.Log.Info(
                $"Currently selected {Plugin.Configuration?.ServerBaseUrl} which is {ServerOptions.Names[selectedIndex]}"
            );
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"Error saving configuration: {e}");
        }
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
            Plugin.CharacterConfiguration.ProfileUID = this.SelectedProfileUID;

            // Save the configuration
            await Plugin.Configuration.Save().ConfigureAwait(false);
            await Plugin.CharacterConfiguration.Save().ConfigureAwait(false);
            // Try to connect to the server
            await _networkService.StartAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error("Login Error Has Occurred", e);
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
