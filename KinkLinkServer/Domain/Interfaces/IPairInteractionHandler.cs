using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkServer.Domain;

namespace KinkLinkServer.Domain.Interfaces;

public record InteractionContext(
    string SenderFriendCode,
    string TargetFriendCode,
    TwoWayPermissions Permissions
);

public interface IPairInteractionHandler
{
    PairAction ActionType { get; }
    Task<ActionResult<Unit>> HandleAsync(
        InteractionContext context,
        InteractionPayload? payload
    );
}
