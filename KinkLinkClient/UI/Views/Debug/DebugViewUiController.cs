using System;
using System.Numerics;
using KinkLinkClient.Dependencies.Honorific.Services;
using KinkLinkClient.Services;
using Newtonsoft.Json;

namespace KinkLinkClient.UI.Views.Debug;

public class DebugViewUiController(
    HonorificService honorific,
    FriendsListService friendsListService,
    NetworkService networkService,
    IdentityService identityService,
    LockService lockService,
    WardrobeService wardrobeService)
{
    public async void Debug()
    {
        try
        {
            //
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"{e}");
        }
    }

    public async void Debug2()
    {
        try
        {
            if (Plugin.ObjectTable.LocalPlayer?.ObjectIndex is not { } index)
                return;

            await honorific.ClearCharacterTitle(index);
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"{e}");
        }
    }
}
