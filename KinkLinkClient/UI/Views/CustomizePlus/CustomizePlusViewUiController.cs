using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinkLinkClient.Dependencies.CustomizePlus.Domain;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Domain;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Customize;

namespace KinkLinkClient.UI.Views.CustomizePlus;

/// <summary>
///     Ui Controller
/// </summary>
public class CustomizePlusViewUiController : IDisposable
{
    // Injected
    private readonly CommandLockoutService _commandLockoutService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly NetworkService _networkService;
    private readonly SelectionManager _selectionManager;

    /// <summary>
    ///     Customize+ data to send
    /// </summary>
    public string SearchTerm = string.Empty;

    /// <summary>
    ///     The currently selected Guid of the Profile to send
    /// </summary>
    public Guid SelectedProfileId = Guid.Empty;

    /// <summary>
    ///     The selected apply mode (Default or Merge)
    /// </summary>
    public CustomizeApplyMode SelectedApplyMode = CustomizeApplyMode.Default;

    private List<Folder<Profile>> _profiles = [];
    public List<Folder<Profile>> FilteredProfiles => SearchTerm == string.Empty
        ? _profiles.ToList()
        : _profiles.Select(folder => new Folder<Profile>(folder.Path, folder.Content.Where(design => design.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList())).ToList();

    /// <summary>
    ///     <inheritdoc cref="CustomizePlusViewUiController"/>
    /// </summary>
    public CustomizePlusViewUiController(CommandLockoutService commandLockoutService, CustomizePlusService customizePlusService, NetworkService networkService, SelectionManager selectionManager)
    {
        _commandLockoutService = commandLockoutService;
        _customizePlusService = customizePlusService;
        _networkService = networkService;
        _selectionManager = selectionManager;

        _customizePlusService.IpcReady += OnIpcReady;
        if (_customizePlusService.ApiAvailable)
            RefreshCustomizeProfiles();
    }

    public async void RefreshCustomizeProfiles()
    {
        try
        {
            SelectedProfileId = Guid.Empty;

            _profiles = await _customizePlusService.GetProfiles().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <summary>
    ///     Calculates the friends who you lack correct permissions to send to
    /// </summary>
    /// <returns></returns>
    public bool MissingPermissionsForATarget()
    {
        return true;
    }

    public async void SendCustomizeProfile()
    {
        try
        {
            if (SelectedProfileId == Guid.Empty)
                return;

            _commandLockoutService.Lock();

            if (await _customizePlusService.GetProfile(SelectedProfileId).ConfigureAwait(false) is not { } profile)
                return;

            var bytes = Encoding.UTF8.GetBytes(profile);
            var request = new CustomizeRequest(_selectionManager.GetSelectedFriendCodes(), bytes, SelectedApplyMode);
            var response = await _networkService.InvokeAsync<ActionResponse>(HubMethod.CustomizePlus, request).ConfigureAwait(false);

            ActionResponseParser.Parse("Customize+", response);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    private void OnIpcReady(object? sender, EventArgs e)
    {
        RefreshCustomizeProfiles();
    }

    public void Dispose()
    {
        _customizePlusService.IpcReady -= OnIpcReady;
        GC.SuppressFinalize(this);
    }
}
