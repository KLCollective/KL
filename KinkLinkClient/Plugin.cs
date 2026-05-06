using System;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Honorific.Services;
using KinkLinkClient.Dependencies.Moodles.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Domain.Configurations;
using KinkLinkClient.Handlers;
using KinkLinkClient.Handlers.Network;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.UI;
using KinkLinkClient.UI.Components.Friends;
using KinkLinkClient.UI.Components.NavigationBar;
using KinkLinkClient.UI.Views.Chat;
using KinkLinkClient.UI.Views.CursedLoot;
using KinkLinkClient.UI.Views.CustomizePlus;
using KinkLinkClient.UI.Views.Debug;
using KinkLinkClient.UI.Views.Emote;
using KinkLinkClient.UI.Views.Friends;
using KinkLinkClient.UI.Views.Gags;
using KinkLinkClient.UI.Views.Games;
using KinkLinkClient.UI.Views.History;
using KinkLinkClient.UI.Views.Honorific;
using KinkLinkClient.UI.Views.Interactions;
using KinkLinkClient.UI.Views.Locks;
using KinkLinkClient.UI.Views.Login;
using KinkLinkClient.UI.Views.Moodles;
using KinkLinkClient.UI.Views.Pause;
using KinkLinkClient.UI.Views.UserProfile;
using KinkLinkClient.UI.Views.Settings;
using KinkLinkClient.UI.Views.Speak;
using KinkLinkClient.UI.Views.Status;
using KinkLinkClient.UI.Views.Wardrobe;
using KinkLinkClient.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace KinkLinkClient;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static ITargetManager TargetManager { get; private set; } = null!;

    [PluginService]
    internal static IObjectTable ObjectTable { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; set; } = null!;

    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; set; } = null!;

    [PluginService]
    internal static INotificationManager NotificationManager { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;
    internal static Configuration Configuration { get; private set; } = null!;
    internal static CharacterConfiguration? CharacterConfiguration { get; set; }
    internal static LegacyConfiguration? LegacyConfiguration { get; private set; }

    /// <summary>
    ///     Internal plugin version
    /// </summary>
    public static readonly Version Version =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

    // Instantiated
    private readonly ServiceProvider _services;

    public Plugin()
    {
        // Load the default configuration
        Configuration =
            ConfigurationService.LoadConfiguration().GetAwaiter().GetResult()
            ?? new Configuration();

        // Create a collection of services
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<ActionQueueService>();
        services.AddSingleton<ClientCharacterStateService>();
        services.AddSingleton<CommandLockoutService>();
        services.AddSingleton<EmoteService>();
        services.AddSingleton<FriendsListService>();
        services.AddSingleton<IdentityService>();
        services.AddSingleton<LogService>();
        services.AddSingleton<LockService>();
        services.AddSingleton<NetworkService>();
        services.AddSingleton<PauseService>();
        services.AddSingleton<PermanentTransformationLockService>();
        services.AddSingleton<ProfileService>();
        services.AddSingleton<TipService>();
        services.AddSingleton<ViewService>();
        services.AddSingleton<WorldService>();

        // Services - Dependencies
        services.AddSingleton<CustomizePlusService>();
        services.AddSingleton<GlamourerService>();
        services.AddSingleton<HonorificService>();
        services.AddSingleton<MoodlesService>();
        services.AddSingleton<PenumbraService>();
        services.AddSingleton<WardrobeService>();
        services.AddSingleton<WardrobeNetworkService>();

        // Managers
        services.AddSingleton<CharacterTransformationManager>();
        services.AddSingleton<ConnectionManager>();
        services.AddSingleton<DependencyManager>();
        services.AddSingleton<LoginManager>();
        services.AddSingleton<PermanentTransformationHandler>();
        services.AddSingleton<SelectionManager>();

        // Handlers
        services.AddSingleton<ChatCommandHandler>();
        services.AddSingleton<GlamourerEventHandler>();

        // Handlers Network
        services.AddSingleton<ChatMessageReceivedHandler>();
        services.AddSingleton<EmoteHandler>();
        services.AddSingleton<HonorificHandler>();
        services.AddSingleton<LockCommandHandler>();
        services.AddSingleton<LockSyncHandler>();
        services.AddSingleton<MoodlesHandler>();
        services.AddSingleton<PairInteractionsHandler>();
        services.AddSingleton<SpeakHandler>();
        services.AddSingleton<SyncOnlineStatusHandler>();
        services.AddSingleton<SyncPairStateHandler>();
        services.AddSingleton<SyncPermissionsHandler>();
        services.AddSingleton<WardrobeSyncHandler>();
        services.AddSingleton<CustomizePlusHandler>();

        // Ui - Component Controllers
        services.AddSingleton<FriendsListComponentUiController>();

        // Ui - Components
        services.AddSingleton<PairsListComponentUi>();
        services.AddSingleton<NavigationBarComponentUi>();

        // Ui - View Controllers
        services.AddSingleton<ChatViewUiController>();
        services.AddSingleton<CustomizePlusViewUiController>();
        services.AddSingleton<DebugViewUiController>();
        services.AddSingleton<EmoteViewUiController>();
        services.AddSingleton<KinkLinkClient.UI.Views.Friends.PairsViewUiController>();
        services.AddSingleton<HistoryViewUiController>();
        services.AddSingleton<HonorificViewUiController>();
        services.AddSingleton<LoginViewUiController>();
        services.AddSingleton<MoodlesViewUiController>();
        services.AddSingleton<PauseViewUiController>();
        services.AddSingleton<ProfileViewUiController>();
        services.AddSingleton<SettingsViewUiController>();
        services.AddSingleton<SpeakViewUiController>();
        services.AddSingleton<StatusViewUiController>();

        services.AddSingleton<CursedLootViewUiController>();
        services.AddSingleton<GagsViewUiController>();
        services.AddSingleton<GamesViewUiController>();
        services.AddSingleton<InteractionsViewUiController>();
        services.AddSingleton<InteractionsViewUiController>();
        services.AddSingleton<LocksViewUiController>();
        services.AddSingleton<WardrobeViewUiController>();

        // Ui - Views
        services.AddSingleton<ChatViewUi>();
        services.AddSingleton<CustomizePlusViewUi>();
        services.AddSingleton<DebugViewUi>();
        services.AddSingleton<EmoteViewUi>();
        services.AddSingleton<KinkLinkClient.UI.Views.Friends.PairsViewUi>();
        services.AddSingleton<HistoryViewUi>();
        services.AddSingleton<HonorificViewUi>();
        services.AddSingleton<LoginViewUi>();
        services.AddSingleton<MoodlesViewUi>();
        services.AddSingleton<PauseViewUi>();
        services.AddSingleton<ProfileViewUi>();
        services.AddSingleton<SettingsViewUi>();
        services.AddSingleton<SpeakViewUi>();
        services.AddSingleton<StatusViewUi>();

        services.AddSingleton<CursedLootViewUi>();
        services.AddSingleton<GagsViewUi>();
        services.AddSingleton<GamesViewUi>();
        services.AddSingleton<InteractionsViewUi>();
        services.AddSingleton<LocksViewUi>();
        services.AddSingleton<WardrobeViewUi>();
        //
        // Ui - Windows
        services.AddSingleton<MainWindow>();
        services.AddSingleton<WindowManager>();

        // Build the dependency injection framework
        _services = services.BuildServiceProvider();

        // Ui - Windows
        _services.GetRequiredService<WindowManager>();

        // Ui - Controllers
        _services.GetRequiredService<LoginViewUiController>(); // Required to display secret once character configuration loads
        _services.GetRequiredService<MoodlesViewUiController>(); // Required to display UI elements when IPCs are loaded
        _services.GetRequiredService<CustomizePlusViewUiController>(); // Required to display UI elements when IPCs are loaded
        _services.GetRequiredService<HonorificViewUiController>(); // Required to display UI elements when IPCs are loaded

        // Handlers
        _services.GetRequiredService<ChatCommandHandler>();
        // _services.GetRequiredService<ChatHandler>();
        _services.GetRequiredService<ConnectionManager>();
        _services.GetRequiredService<GlamourerEventHandler>();

        // Handlers Network
        _services.GetRequiredService<ChatMessageReceivedHandler>();
        _services.GetRequiredService<EmoteHandler>();
        _services.GetRequiredService<HonorificHandler>();
        _services.GetRequiredService<LockCommandHandler>();
        _services.GetRequiredService<LockSyncHandler>();
        _services.GetRequiredService<MoodlesHandler>();
        _services.GetRequiredService<PairInteractionsHandler>();
        _services.GetRequiredService<SpeakHandler>();
        _services.GetRequiredService<SyncOnlineStatusHandler>();
        _services.GetRequiredService<SyncPairStateHandler>();
        _services.GetRequiredService<SyncPermissionsHandler>();
        _services.GetRequiredService<WardrobeSyncHandler>();
        _services.GetRequiredService<CustomizePlusHandler>();

        // Managers
        _services.GetRequiredService<DependencyManager>();
        _services.GetRequiredService<LoginManager>();

        // Services
        _services.GetRequiredService<ActionQueueService>();

        // Wire up services
        _services
            .GetRequiredService<WardrobeNetworkService>()
            .SetWardrobeService(_services.GetRequiredService<WardrobeService>());

        Task.Run(SharedUserInterfaces.InitializeFonts);
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    /// <summary>
    ///     Runs provided function on the XIV Framework. Await should never be used inside the <see cref="Func{T}"/>
    ///     passed to this function.
    /// </summary>
    public static async Task<T> RunOnFramework<T>(Func<T> func)
    {
        if (Framework.IsInFrameworkUpdateThread)
            return func.Invoke();

        return await Framework.RunOnFrameworkThread(func).ConfigureAwait(false);
    }

    public static async Task RunOnFramework(Action func)
    {
        if (Framework.IsInFrameworkUpdateThread)
        {
            func.Invoke();
        }
        else
        {
            await Framework.RunOnFrameworkThread(func).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Runs provided function on the XIV Framework. Await should never be used inside the <see cref="Func{T}"/>
    ///     passed to this function.
    /// </summary>
    public static async Task<T> RunOnFrameworkSafely<T>(Func<T> func)
    {
        try
        {
            if (Framework.IsInFrameworkUpdateThread)
                return func.Invoke();

            return await Framework.RunOnFrameworkThread(func).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Warning($"[DependencyManager.RunOnFrameworkSafely] An error occurred, {e}");
            return Activator.CreateInstance<T>();
        }
    }

    /*
     *  AR Supporters Name-Game
     *  =======================
     *  I want to show appreciation for those who were here in the beginning, supporting both the plugin
     *  and I unconditionally. There have been a lot of tough challenges and fun moments,
     *  but you all helped me preserve and that deserves recognition.
     *  So I've decided to immorality all those names in the plugin code; Not as comments, but as actual variables!
     *  Below is a list of everyone who will slowly be phased into variable names, see if you can spot where they appear
     *  in future commits! I'm looking at you, Tezra.
     *  Much love to every name on this list. If I missed anyone, PLEASE LET ME KNOW. There were a lot of people to comb
     *  through, and I may have missed a name or two.
     *  =======================
     *  Aria
     *  Asami
     *  Cami
     *  Clarjii
     *  Cleichant
     *  Damy
     *  Delilah
     *  Dub
     *  Etche
     *  Eleanora
     *  Ferra
     *  Kaga
     *  Kari
     *  Kerc
     *  Leona
     *  Mae
     *  Misty
     *  Miyuki
     *  Mylla
     *  Neith
     *  Norg
     *  Pet
     *  Pris
     *  Red
     *  Rosalyne
     *  Silent
     *  Soph
     *  Suzy
     *  Tezra
     *  Tixa/Dolly
     *  Traia
     *  Vanessa
     *  Yilana
     */
}
