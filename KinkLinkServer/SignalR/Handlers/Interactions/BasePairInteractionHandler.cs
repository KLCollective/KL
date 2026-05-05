using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;

namespace KinkLinkServer.SignalR.Handlers.Interactions;

public abstract class BasePairInteractionHandler(
    LocksHandler locksHandler,
    KinkLinkProfilesService profilesService,
    ILogger logger
) : IPairInteractionHandler
{
    protected readonly LocksHandler _locksHandler = locksHandler;
    protected readonly KinkLinkProfilesService _profilesService = profilesService;
    protected readonly ILogger _logger = logger;

    public abstract PairAction ActionType { get; }

    public abstract Task<ActionResult<Unit>> HandleAsync(
        InteractionContext context,
        InteractionPayload? payload
    );

    protected async Task<int?> GetTargetProfileIdAsync(string targetFriendCode)
    {
        var targetProfileId = await _profilesService.GetProfileIdFromUidAsync(targetFriendCode);
        return targetProfileId;
    }
}
