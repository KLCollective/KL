using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Services;

namespace KinkLinkClient.Domain.Configurations;

/// <summary>
///     The global configuration
/// </summary>
[Serializable]
public class Configuration
{
    /// <summary>
    ///     Configuration version
    /// </summary>
    public int Version = 1;

    /// <summary>
    ///     Base URL for the KinkLink server (e.g., "http://localhost:5006")
    /// </summary>
    public string ServerBaseUrl = "http://localhost:5006";

    /// <summary>
    ///     Used as a global toggle to ensure that all modules are disabled regardless of their settings.
    /// </summary>
    public bool SafeMode = false;

    /// <summary>
    ///     This is the authentication secret key provided by the disord bot for authorization
    /// </summary>
    public string SecretKey = "";

    /// <summary>
    ///     Map of friend code to note
    /// </summary>
    public Dictionary<string, string> Notes = [];

    /// <summary>
    ///     Save the configuration
    /// </summary>
    public async Task Save() =>
        await ConfigurationService.SaveConfiguration(this).ConfigureAwait(false);
}
