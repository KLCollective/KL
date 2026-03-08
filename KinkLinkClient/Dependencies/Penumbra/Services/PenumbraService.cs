using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Utils;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace KinkLinkClient.Dependencies.Penumbra.Services;

/// <summary>
///     Provides access to Penumbra
/// </summary>
public class PenumbraService : IExternalPlugin
{
    // Const
    private const string TemporaryModName = "KinkLinkMods";
    private const int Priority = int.MaxValue - 32; // Give other mods the opportunity to override if needed;

    private const string KINKLINK_ID = "Dollhouse";
    private const int KINKLINK_KEY = 0xD011;
    private const int PLAYER_ID = 0;

    // Const
    private const int ExpectedMajor = 4;

    // Penumbra API required by KinkLink
    private readonly QueryTemporaryModSettingsPlayer _queryTemporaryMod;
    private readonly SetTemporaryModSettingsPlayer _setTemporaryModPlayer;
    private readonly RemoveTemporaryModSettingsPlayer _removeTemporaryModSettingsPlayer; // Removes temporary mod settings from the player
    private readonly RemoveAllTemporaryModSettingsPlayer _removeAllTemporaryModSettingsPlayer;
    private readonly GetModList _getModList;
    private readonly GetCurrentModSettings _getCurrentModSettings;

    // Prior AetherRemote IPC
    private readonly AddTemporaryMod _addTemporaryMod;
    private readonly GetGameObjectResourcePaths _getGameObjectResourcePaths;
    private readonly GetMetaManipulations _getMetaManipulations;
    private readonly GetCollectionForObject _getCollectionForObject;
    private readonly RemoveTemporaryMod _removeTemporaryMod;
    private readonly RedrawObject _redrawObject;
    private readonly ApiVersion _version;

    /// <summary>
    ///     Is Penumbra available for use?
    /// </summary>
    public bool ApiAvailable;

    /// <summary>
    ///     <inheritdoc cref="IExternalPlugin.IpcReady"/>
    /// </summary>
    public event EventHandler? IpcReady;

    /// <summary>
    ///     <see cref="PenumbraService"/>
    /// </summary>
    public PenumbraService()
    {
        _setTemporaryModPlayer = new SetTemporaryModSettingsPlayer(Plugin.PluginInterface);
        _queryTemporaryMod = new QueryTemporaryModSettingsPlayer(Plugin.PluginInterface);
        _removeTemporaryModSettingsPlayer = new RemoveTemporaryModSettingsPlayer(
            Plugin.PluginInterface
        );
        _removeAllTemporaryModSettingsPlayer = new RemoveAllTemporaryModSettingsPlayer(
            Plugin.PluginInterface
        );
        _getModList = new GetModList(Plugin.PluginInterface);
        _getCurrentModSettings = new GetCurrentModSettings(Plugin.PluginInterface);

        _addTemporaryMod = new AddTemporaryMod(Plugin.PluginInterface);
        _getGameObjectResourcePaths = new GetGameObjectResourcePaths(Plugin.PluginInterface);
        _getMetaManipulations = new GetMetaManipulations(Plugin.PluginInterface);
        _removeTemporaryMod = new RemoveTemporaryMod(Plugin.PluginInterface);
        _getCollectionForObject = new GetCollectionForObject(Plugin.PluginInterface);
        _redrawObject = new RedrawObject(Plugin.PluginInterface);
        _version = new ApiVersion(Plugin.PluginInterface);
    }

    /// <summary>
    ///     Tests for availability to Penumbra
    /// </summary>
    public async Task<bool> TestIpcAvailability()
    {
        // Set everything to disabled state
        ApiAvailable = false;

        // Invoke Api
        var version = await Plugin
            .RunOnFrameworkSafely(() => _version.Invoke())
            .ConfigureAwait(false);

        // Test for proper versioning
        if (version.Breaking < ExpectedMajor)
            return false;

        // Mark as ready
        ApiAvailable = true;
        IpcReady?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task<List<(Mod, ModSettings)>> GetAllMods()
    {
        if (!ApiAvailable)
        {
            Plugin.Log.Info($"[PenumbraService] Api is not available");
            return new();
        }
        List<(Mod, ModSettings)> settings = await Plugin.RunOnFramework(() =>
        {
            // Excessive comment because this is a wonky one and the intent may not be clear.
            // 1. Grab the current collection for the current player character (this can be many collections, but the one associated with the player is what we'll support for now)
            // 2. Grab all the mods listed in penumbra
            // 3. Grab all the valid settings for the mods that we can find.
            // 4. Sort through the settings to return only the ones that are valid.
            var collection = _getCollectionForObject!.Invoke(PLAYER_ID).EffectiveCollection.Id;
            var allMods = _getModList.Invoke();
            var modSettings = new List<(Mod, ModSettings)>();
            return allMods
                .Select(m =>
                    (m.Key, m.Value, _getCurrentModSettings.Invoke(collection, m.Key, m.Value))
                )
                .Where(item =>
                    item.Item3.Item1 is PenumbraApiEc.Success && item.Item3.Item2 != null
                )
                // Conversion to our `Mod`
                .Select(item =>
                    (
                        new Mod(
                            // Mod Name
                            item.Item2,
                            // Mod Directory
                            item.Item1
                        ),
                        new ModSettings(
                            // Mod Settings
                            item.Item3.Item2!.Value.Item3,
                            // Mod priority
                            item.Item3.Item2.Value.Item2,
                            // Mod enabled
                            item.Item3.Item2!.Value.Item1
                        )
                    )
                )
                .ToList();
        });
        if (settings.Count == 0)
            Plugin.Log.Warning($"[PenumbraService] Could not find any mod data");
        return settings;
    }

    public async Task SetTemporaryModState(Mod mod, ModSettings settings, bool enabled)
    {
        if (!ApiAvailable)
        {
            Plugin.Log.Info($"[PenumbraService] ApiIs not available");
            return;
        }

        try
        {
            var collection = _getCollectionForObject!.Invoke(PLAYER_ID).EffectiveCollection.Id;

            if (enabled)
            {
                var errorCode = _setTemporaryModPlayer.Invoke(
                    PLAYER_ID,
                    mod.DirectoryName,
                    false,
                    true,
                    settings.Priority + 100,
                    settings.Settings.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (IReadOnlyList<string>)kvp.Value
                    ),
                    KINKLINK_ID,
                    KINKLINK_KEY,
                    mod.Name
                );
                switch (errorCode)
                {
                    case PenumbraApiEc.ModMissing:
                        Plugin.Log.Info(
                            $"[PenumbraService] Mod {mod.Name}, {mod.DirectoryName} is missing"
                        );
                        return;
                    case PenumbraApiEc.CollectionMissing:
                        Plugin.Log.Info($"[PenumbraService] Collection is Missing");
                        return;
                }
            }
            // If we wanted to disable the mod, and we are not only adjusting priority, disable it.
            else
            {
                // Adjust the priority of the mod back to its original value
                var errorCode = _removeTemporaryModSettingsPlayer!.Invoke(
                    PLAYER_ID,
                    mod.DirectoryName,
                    KINKLINK_KEY,
                    mod.Name
                );
                if (errorCode is not PenumbraApiEc.Success and not PenumbraApiEc.NothingChanged)
                    Plugin.Log.Info($"[PenumbraService] Unknown error occurred");
                return;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error modifying mod state in Penumbra:\n{ex}");
            return;
        }
    }

    public void ClearAllTemporaryMods()
    {
        if (!ApiAvailable)
        {
            Plugin.Log.Info($"[PenumbraService] Api is not available");
            return;
        }
        else
        {
            _removeAllTemporaryModSettingsPlayer!.Invoke(PLAYER_ID, KINKLINK_KEY);
        }
    }

    /// <summary>
    ///     Calls penumbra's GetGameObjectResourcePaths function
    /// </summary>
    /// <returns>A list of modified objects, mapping the modified object to the target path</returns>
    public async Task<Dictionary<string, string>> GetGameObjectResourcePaths(ushort index)
    {
        // TODO: There is an issue with multiple named files having the same name causing issues
        //          An example of which would be if someone has ../../something_SOMETHING.tex
        //          and ../../something_something.tex
        //          There is a possibility of fixing this just by forcing everything lowercase
        //          however I don't know enough about mod paths to know if this is problematic

        if (ApiAvailable)
            return await Plugin
                .RunOnFramework(() =>
                {
                    try
                    {
                        var resources = _getGameObjectResourcePaths.Invoke(index);
                        var paths = new Dictionary<string, string>();
                        foreach (var resource in resources)
                        {
                            if (resource is null)
                                continue;

                            foreach (var kvp in resource)
                            {
                                foreach (var item in kvp.Value)
                                {
                                    if (item.EndsWith(".imc") || kvp.Key.EndsWith(".imc"))
                                    {
                                        Plugin.Log.Verbose(
                                            $"Skipping .imc redirect {item} --> {kvp.Key}"
                                        );
                                        continue;
                                    }

                                    paths.TryAdd(item, kvp.Key);
                                }
                            }
                        }

                        return paths;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Warning(
                            $"[PenumbraService] Unexpectedly failed getting resource paths for index {index}, {e.Message}"
                        );
                        return [];
                    }
                })
                .ConfigureAwait(false);

        Plugin.Log.Warning(
            $"[PenumbraService] Failed to get object resource paths for index {index} because penumbra is not available"
        );
        return [];
    }

    /// <summary>
    ///     Calls penumbra's GetMetaManipulations function
    /// </summary>
    /// <returns>A character's metadata</returns>
    public async Task<string> GetMetaManipulations(ushort index)
    {
        if (ApiAvailable)
            return await Plugin
                .RunOnFramework(() =>
                {
                    try
                    {
                        return _getMetaManipulations.Invoke(index);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Warning(
                            $"[PenumbraService] Unexpectedly failed getting manipulations for index {index}, {e.Message}"
                        );
                        return string.Empty;
                    }
                })
                .ConfigureAwait(false);

        Plugin.Log.Warning(
            $"[PenumbraService] Failed to get manipulations for index {index} because penumbra is not available"
        );
        return string.Empty;
    }

    /// <summary>
    ///     Calls penumbra's GetCollectionForObject function
    /// </summary>
    /// <param name="index">Object table index of the collection to get, defaulted to the local player's</param>
    /// <returns>Collection GUID</returns>
    public async Task<Guid> GetCollection(int index = 0)
    {
        if (ApiAvailable)
            return await Plugin
                .RunOnFramework(() =>
                {
                    try
                    {
                        return _getCollectionForObject.Invoke(index).EffectiveCollection.Id;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Warning(
                            $"[PenumbraService] Unexpectedly failed to get collection for index {index}, {e.Message}"
                        );
                        return Guid.Empty;
                    }
                })
                .ConfigureAwait(false);

        Plugin.Log.Warning(
            $"[PenumbraService] Failed to get collection for index {index} because penumbra is not available"
        );
        return Guid.Empty;
    }

    /// <summary>
    ///     Calls penumbra's AddTemporaryMod function
    /// </summary>
    /// <returns><see cref="bool"/> indicating success</returns>
    public async Task<bool> AddTemporaryMod(
        Guid collectionGuid,
        Dictionary<string, string> modifiedPaths,
        string meta
    )
    {
        if (ApiAvailable)
            return await Plugin
                .RunOnFramework(() =>
                {
                    try
                    {
                        var result = _addTemporaryMod.Invoke(
                            TemporaryModName,
                            collectionGuid,
                            modifiedPaths,
                            meta,
                            Priority
                        );
                        switch (result)
                        {
                            case PenumbraApiEc.Success:
                                return true;

                            case PenumbraApiEc.InvalidGamePath:
                                NotificationHelper.Error(
                                    "Invalid Game Files",
                                    "The person you are being transformed into contains mods that do not all have valid game file paths. Most commonly, this happens when a mod maker includes two or more assets with the same name, but different punctuation: \"aether_remote.png\" and \"Aether_Remote.png\" as an example."
                                );
                                break;
                        }

                        Plugin.Log.Warning(
                            $"[PenumbraService] Adding temporary mod was unsuccessful, result was {result}"
                        );
                        return false;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Warning(
                            $"[PenumbraService] Adding temporary mod failed unexpectedly, {e.Message}"
                        );
                        return false;
                    }
                })
                .ConfigureAwait(false);

        Plugin.Log.Warning(
            "[PenumbraService] Unable to add temporary mod because penumbra is not available"
        );
        return false;
    }

    /// <summary>
    ///     Calls penumbra's RemoveTemporaryMod function
    /// </summary>
    /// <returns><see cref="bool"/> indicating success</returns>
    public async Task<bool> CallRemoveTemporaryMod(Guid collectionId)
    {
        if (ApiAvailable)
            return await Plugin
                .RunOnFramework(() =>
                {
                    try
                    {
                        var result = _removeTemporaryMod.Invoke(
                            TemporaryModName,
                            collectionId,
                            Priority
                        );
                        if (result is PenumbraApiEc.Success or PenumbraApiEc.NothingChanged)
                            return true;

                        Plugin.Log.Warning(
                            $"[PenumbraService] Removing temporary mod was unsuccessful, result was {result}"
                        );
                        return false;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Warning(
                            $"[PenumbraService] Removing temporary mod failed unexpectedly, {e.Message}"
                        );
                        return false;
                    }
                })
                .ConfigureAwait(false);

        Plugin.Log.Warning(
            "[PenumbraService] Unable to remove temporary mod because penumbra is not available"
        );
        return false;
    }

    /// <summary>
    ///     Redraws the local client
    /// </summary>
    public async Task<bool> CallRedraw(int objectIndex = 0)
    {
        if (ApiAvailable)
            return await Plugin
                .RunOnFramework(() =>
                {
                    try
                    {
                        _redrawObject.Invoke(objectIndex);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Warning(
                            $"[PenumbraService] Redrawing failed unexpectedly, {e.Message}"
                        );
                        return false;
                    }
                })
                .ConfigureAwait(false);

        Plugin.Log.Warning("[PenumbraService] Unable to redraw because penumbra is not available");
        return false;
    }
}
