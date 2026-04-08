using System.Numerics;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.UI.Components.NavigationBar;
using KinkLinkClient.UI.Views.CustomizePlus;
using KinkLinkClient.UI.Views.Debug;
using KinkLinkClient.UI.Views.Emote;
using KinkLinkClient.UI.Views.Friends;
using KinkLinkClient.UI.Views.History;
using KinkLinkClient.UI.Views.Honorific;
using KinkLinkClient.UI.Views.Login;
using KinkLinkClient.UI.Views.Moodles;
using KinkLinkClient.UI.Views.Pause;
using KinkLinkClient.UI.Views.UserProfile;
using KinkLinkClient.UI.Views.Settings;
using KinkLinkClient.UI.Views.Speak;
using KinkLinkClient.UI.Views.Status;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using KinkLinkClient.UI.Views.Chat;
using KinkLinkClient.UI.Views.CursedLoot;
using KinkLinkClient.UI.Views.Gags;
using KinkLinkClient.UI.Views.Games;
using KinkLinkClient.UI.Views.Interactions;
using KinkLinkClient.UI.Views.Locks;
using KinkLinkClient.UI.Views.Wardrobe;

namespace KinkLinkClient.UI;

public class MainWindow : Window
{
    // Const
    private static readonly string MainWindowTitle = $"Kink Link 2 - Version {Plugin.Version}";

    // Services
    private readonly ViewService _viewService;

    // Components
    private readonly NavigationBarComponentUi _navigationBar;

    // Services

    // Views
    private readonly ChatViewUi _chatView;
    private readonly CustomizePlusViewUi _customizePlusView;
    private readonly DebugViewUi _debugView;
    private readonly EmoteViewUi _emoteView;
    private readonly PairsViewUi _friendsView;
    private readonly HistoryViewUi _historyView;
    private readonly HonorificViewUi _honorificView;
    private readonly LoginViewUi _loginView;
    private readonly MoodlesViewUi _moodlesView;
    private readonly PauseViewUi _pauseView;
    private readonly ProfileViewUi _profileView;
    private readonly SettingsViewUi _settingsView;
    private readonly SpeakViewUi _speakView;
    private readonly StatusViewUi _statusView;
    private readonly CursedLootViewUi _cursedlootView;
    private readonly GagsViewUi _gagsView;
    private readonly GamesViewUi _gamesView;
    private readonly InteractionsViewUi _interactionsView;
    private readonly LocksViewUi _locksView;
    private readonly WardrobeViewUi _wardrobeView;

    public MainWindow(
        ViewService viewService,
        NavigationBarComponentUi navigationBarComponentUi,
        ChatViewUi chatUiService,
        CustomizePlusViewUi customizePlusView,
        DebugViewUi debugView,
        EmoteViewUi emoteView,
        PairsViewUi friendsView,
        HistoryViewUi historyView,
        HonorificViewUi honorificView,
        LoginViewUi loginView,
        MoodlesViewUi moodlesView,
        PauseViewUi pauseView,
        ProfileViewUi profileView,
        SettingsViewUi settingsView,
        SpeakViewUi speakView,
        StatusViewUi statusView,
         CursedLootViewUi cursedlootView,
         GagsViewUi gagsView,
         GamesViewUi gamesView,
         InteractionsViewUi interactionsView,
         LocksViewUi locksView,
         WardrobeViewUi wardrobeView
        ) : base(MainWindowTitle)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 500),
            MaximumSize = ImGui.GetIO().DisplaySize
        };

        _viewService = viewService;
        _chatView = chatUiService;

        _navigationBar = navigationBarComponentUi;

        _customizePlusView = customizePlusView;
        _debugView = debugView;
        _emoteView = emoteView;
        _friendsView = friendsView;
        _historyView = historyView;
        _honorificView = honorificView;
        _loginView = loginView;
        _moodlesView = moodlesView;
        _pauseView = pauseView;
        _profileView = profileView;
        _settingsView = settingsView;
        _speakView = speakView;
        _statusView = statusView;
        _cursedlootView = cursedlootView;
        _gagsView = gagsView;
        _gamesView = gamesView;
        _interactionsView = interactionsView;
        _locksView = locksView;
        _wardrobeView = wardrobeView;
    }

    public override void Draw()
    {
        _navigationBar.Draw();

        ImGui.SameLine();

        IDrawable view = _viewService.CurrentView switch
        {
            View.CursedLoot => _cursedlootView,
            View.Chat => _chatView,
            View.Debug => _debugView,
            View.Emote => _emoteView,
            View.Gags => _gagsView,
            View.Games => _gamesView,
            View.History => _historyView,
            View.Honorific => _honorificView,
            View.Interactions => _interactionsView,
            View.Locks => _locksView,
            View.Login => _loginView,
            View.Moodles => _moodlesView,
            View.Pairs => _friendsView,
            View.Pause => _pauseView,
            View.Profile => _profileView,
            View.Settings => _settingsView,
            View.Status => _statusView,
            View.Wardrobe => _wardrobeView,
            _ => _loginView
        };

        view.Draw();
    }
}
