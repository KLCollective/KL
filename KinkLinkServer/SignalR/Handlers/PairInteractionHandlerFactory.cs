using System.Collections.Immutable;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Handlers.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace KinkLinkServer.SignalR.Handlers;

public interface IPairInteractionHandlerFactory
{
    IPairInteractionHandler? GetHandler(PairAction action);
}

public class PairInteractionHandlerFactory(IServiceProvider serviceProvider)
    : IPairInteractionHandlerFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public IPairInteractionHandler? GetHandler(PairAction action)
    {
        switch (action)
        {
            case PairAction.LockWardrobe:
                return _serviceProvider.GetRequiredService<LockWardrobeInteractionHandler>();
            case PairAction.UnlockWardrobe:
                return _serviceProvider.GetRequiredService<UnlockWardrobeInteractionHandler>();
            case PairAction.ApplyWardrobe:
                return _serviceProvider.GetRequiredService<WardrobeApplyInteractionHandler>();
        }

        return null;
    }
}
