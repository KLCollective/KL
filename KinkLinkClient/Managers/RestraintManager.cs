using KinkLinkClient.Utils;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Honorific.Services;
using KinkLinkClient.Dependencies.Moodles.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Services;
using System.Threading.Tasks;

namespace KinkLinkClient.Managers;

/// Controls the end to end process for each restraint function call.
public class RestraintManager (
    GlamourerService glamourerService,
    HonorificService honorificService,
    MoodlesService moodlesService,
    PenumbraService penumbraService,
    PermanentTransformationLockService locksService,
    WardrobeService wardrobeService,
    GarblerService garblerService,
    NetworkService networkService){

    public async Task SetRestraintSet(bool locked=false) {
        NotificationHelper.Info("SetRestraintSet", "Called");
        // TODO: Restraint set should call the wardrobeService to apply the data
    }
    // TODO: Layer should be enough
    public async Task SetGag(int layer, bool locked=false) {
        NotificationHelper.Info("SetRestraintSet", "Called");
    }

    public async Task SetRestraintItem(bool locked=false) {
        NotificationHelper.Info("SetRestraintSet", "Called");
    }
}
