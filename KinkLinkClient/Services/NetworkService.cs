using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.GetProfiles;
using KinkLinkCommon.Domain.Network.GetToken;
using KinkLinkCommon.Domain.Network.LoginAuthentication;
using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace KinkLinkClient.Services;

/// <summary>
///     Provides methods to interact with the server
/// </summary>
public class NetworkService : IDisposable
{
    /// <summary>
    ///     The Signal R connection
    /// </summary>
    public HubConnection? _connection = null;
    public HubConnection Connection
    {
        // It should not be possible to try to connect _before_ a connection has been made.
        get { return _connection!; }
    }

    /// <summary>
    ///     Event fired when the server successfully connects, either by reconnection or manual connection
    /// </summary>
    public event Func<Task>? Connected;

    /// <summary>
    ///     Event fired when the server connection is lost, either by disruption or manual intervention
    /// </summary>
    public event Func<Task>? Disconnected;

    /// <summary>
    ///     Gets the full hub URL by combining server base URL with hub route
    /// </summary>
    private string HubUrl =>
        $"{Plugin.Configuration?.ServerBaseUrl ?? "http://localhost:5006"}/primaryHub";

    /// <summary>
    ///     Gets the full profiles URL by combining server base URL with profiles route
    /// </summary>
    private string ProfilesUrl =>
        $"{Plugin.Configuration?.ServerBaseUrl ?? "http://localhost:5006"}/api/auth/profiles";

    /// <summary>
    ///     Gets the full auth URL by combining server base URL with login route
    /// </summary>
    private string AuthUrl =>
        $"{Plugin.Configuration?.ServerBaseUrl ?? "http://localhost:5006"}/api/auth/login";

    /// <summary>
    ///     Fetches the list of profiles associated with a secret
    /// </summary>
    public async Task<List<(string Uid, string Alias)>> GetProfilesAsync(string secret)
    {
        try
        {
            using var client = new HttpClient();
            var request = new GetProfilesRequest(secret);
            var payload = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(ProfilesUrl, payload).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Plugin.Log.Information(
                "[GetProfilesAsync] Response status: {Status}, content: {Content}",
                response.StatusCode,
                content
            );

            var result = JsonSerializer.Deserialize<GetProfilesResult>(
                content,
                DeserializationOptions
            );

            var profiles = result?.Profiles?.Select(p => (p.Uid, p.Alias)).ToList() ?? [];

            Plugin.Log.Information(
                "[GetProfilesAsync] Deserialized profiles: {Count}",
                profiles.Count
            );

            return profiles;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "[NetworkService.GetProfilesAsync] Error");
            return [];
        }
    }

    /// <summary>
    /// To fix deserialization that are occurring when connecting, not sure why, but C# appears to have issues with serializing/deserializing a JSON string via message pack without it.
    /// </summary>
    private static readonly JsonSerializerOptions DeserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    ///     Access token required to connect to the SignalR hub
    /// </summary>
    private string? _token = string.Empty;

    /// <summary>
    ///     If the plugin has begun the connection process
    /// </summary>
    public bool Connecting;

    /// <summary>
    ///     <inheritdoc cref="NetworkService"/>
    /// </summary>
    public NetworkService()
    {
        // Try to build the connection based on last configuration
        try
        {
            buildConnection();
        }
        catch (Exception e)
        {
            Plugin.Log.Warning(
                $"[NetworkService] Building default connection failed with: {e.Message}",
                e
            );
        }
    }

    public async void SetBaseURL(string url)
    {
        Plugin.Configuration.ServerBaseUrl = url;
        await Plugin.Configuration.Save();
    }

    public void buildConnection()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(
                HubUrl,
                options =>
                {
                    // ReSharper disable once RedundantTypeArgumentsOfMethod
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_token);
                }
            )
            .WithAutomaticReconnect()
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithSecurity(
                    MessagePackSecurity.UntrustedData
                );
            })
            .Build();

        Connection.Reconnected += OnReconnected;
        Connection.Reconnecting += OnReconnecting;
        Connection.Closed += OnClosed;
    }

    /// <summary>
    ///     Begins a connection to the server
    /// </summary>
    public async Task StartAsync()
    {
        if (Connection.State is not HubConnectionState.Disconnected)
        {
            Plugin.Log.Verbose(
                "[NetworkService] Network connection is pending or already established"
            );
            return;
        }

        Connecting = true;

        try
        {
            // Try to get the Token
            if (await TryAuthenticateSecret().ConfigureAwait(false) is { } token)
            {
                _token = token;

                await Connection.StartAsync().ConfigureAwait(false);

                if (Connection.State is HubConnectionState.Connected)
                {
                    Connected?.Invoke();
                    NotificationHelper.Success("[Kink Link] Connected", string.Empty);
                }
                else
                {
                    NotificationHelper.Warning(
                        "[Kink Link] Unable to connect",
                        "See developer console for more information"
                    );
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[NetworkService] [StartAsync] {e.Message}]");
            NotificationHelper.Warning(
                "[Kink Link] Could not connect",
                "See developer console for more information"
            );
        }

        Connecting = false;
    }

    /// <summary>
    ///     Ends a connection to the server
    /// </summary>
    public async Task StopAsync()
    {
        if (Connection.State is HubConnectionState.Disconnected)
        {
            Plugin.Log.Verbose("[NetworkService] Network connection is already disconnected");
            return;
        }

        try
        {
            await Connection.StopAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[NetworkService] [StopAsync] Error, {e.Message}]");
        }
    }

    /// <summary>
    ///     Invokes a method on the server and awaits a result
    /// </summary>
    /// <param name="method">The name of the method to call</param>
    /// <param name="request">The request object to send</param>
    /// <returns></returns>
    public async Task<T> InvokeAsync<T>(string method, object request)
    {
        if (Connection.State is not HubConnectionState.Connected)
        {
            Plugin.Log.Warning("[NetworkService] No connection established");
            return default!;
        }

        try
        {
            Plugin.Log.Verbose($"[NetworkService] Request: {request}");
            var response = await Connection.InvokeAsync<T>(method, request);
            Plugin.Log.Verbose($"[NetworkService] Response: {response}");
            return response;
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[NetworkService] [InvokeAsync] {request} {e}");
            return default!;
        }
    }

    /// <summary>
    ///     Invokes a method on the server with no arguments and awaits a result
    /// </summary>
    /// <param name="method">The name of the method to call</param>
    /// <returns></returns>
    public async Task<T> InvokeAsync<T>(string method)
    {
        if (Connection.State is not HubConnectionState.Connected)
        {
            Plugin.Log.Warning("[NetworkService] No connection established");
            return default!;
        }

        try
        {
            var response = await Connection.InvokeAsync<T>(method);
            Plugin.Log.Verbose($"[NetworkService] Response: {response}");
            return response;
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[NetworkService] [InvokeAsync] {method} {e}");
            return default;
        }
    }

    private Task OnReconnected(string? arg)
    {
        Connected?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnClosed(Exception? arg)
    {
        Disconnected?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? arg)
    {
        Disconnected?.Invoke();
        return Task.CompletedTask;
    }

    private async Task<string?> TryAuthenticateSecret()
    {
        if (Plugin.Configuration is null || Plugin.CharacterConfiguration is null)
        {
            Plugin.Log.Warning(
                "[NetworkService.TryAuthenticateSecret] No configuration available to attempt to authenticate with"
            );
            return null;
        }

        if (
            string.IsNullOrWhiteSpace(Plugin.Configuration.SecretKey)
            || string.IsNullOrWhiteSpace(Plugin.CharacterConfiguration.ProfileUID)
        )
        {
            Plugin.Log.Warning(
                "[NetworkService.TryAuthenticateSecret] Secret Key or UID is missing for authentication"
            );
            return null;
        }
        using var client = new HttpClient();
        Plugin.Log.Info(
            $"[NetworkService.TryAuthenticateSecret] Attempting authentication for ProfileUID: {Plugin.CharacterConfiguration.ProfileUID}"
        );
        var request = new GetTokenRequest(
            Plugin.Configuration.SecretKey,
            Plugin.CharacterConfiguration.ProfileUID,
            Plugin.Version
        );

        var payload = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await client.PostAsync(AuthUrl, payload).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (
                JsonSerializer.Deserialize<LoginAuthenticationResult>(
                    content,
                    DeserializationOptions
                )
                is not { } result
            )
            {
                Plugin.Log.Warning(
                    "[NetworkService.TryAuthenticateSecret] A deserialization error occurred"
                );
                return null;
            }

            switch (result.ErrorCode)
            {
                case LoginAuthenticationErrorCode.Success:
                    return result.Token;

                case LoginAuthenticationErrorCode.VersionMismatch:
                    Plugin.Log.Warning(
                        $"[NetworkService] [LoginAuthenticationError] Client Outdated]"
                    );
                    NotificationHelper.Error(
                        "Kink Link - Client Outdated",
                        "You will need to update the plugin before connecting to the servers."
                    );
                    return null;

                case LoginAuthenticationErrorCode.UnknownSecret:
                    Plugin.Log.Warning(
                        $"[NetworkService] [LoginAuthenticationError] Invalid secret"
                    );
                    NotificationHelper.Error(
                        "Kink Link - Invalid Secret",
                        "The secret you provided is either empty, or invalid. If you believe this is a mistake, please reach out to the developer."
                    );
                    return null;

                case LoginAuthenticationErrorCode.UnknownProfileUID:
                    Plugin.Log.Warning(
                        $"[NetworkService] [LoginAuthenticationError] Invalid ProfileUID]"
                    );
                    NotificationHelper.Error(
                        "Kink Link - Invalid ProfileUID",
                        "The secret you provided is either empty, or invalid. If you believe this is a mistake, please reach out to the developer."
                    );
                    return null;

                case LoginAuthenticationErrorCode.Uninitialized:
                case LoginAuthenticationErrorCode.Unknown:
                default:
                    Plugin.Log.Warning(
                        $"[NetworkService] [LoginAuthenticationError] Unable to connect for some reason"
                    );
                    NotificationHelper.Error(
                        "Kink Link - Unable to Connect",
                        $"Something went wrong while connecting to the server, {result.ErrorCode}"
                    );
                    return null;
            }
        }
        catch (HttpRequestException e)
        {
            Plugin.Log.Error($"[NetworkService] [HttpRequestException] {e.Message}]");
            NotificationHelper.Warning(
                "Authentication Server Down",
                "Please wait and try again later. You can monitor or report this problem in the discord if it persists"
            );
            return null;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.ToString());

            return null;
        }
    }

    public void Dispose()
    {
        Connection.Reconnected -= OnReconnected;
        Connection.Reconnecting -= OnReconnecting;
        Connection.Closed -= OnClosed;

        Connection.StopAsync().ConfigureAwait(false);
        Connection.DisposeAsync().AsTask().GetAwaiter().GetResult();

        GC.SuppressFinalize(this);
    }
}
