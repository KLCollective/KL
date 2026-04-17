using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.GetAccountData;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.GetAccountData)]
    public async Task<GetAccountDataResponse> GetAccountData(GetAccountDataRequest request)
    {
        logger.LogInformation("[SignalR] GetAccountData: {FriendCode}", FriendCode);
        return await getAccountDataHandler.Handle(FriendCode, Context.ConnectionId, request);
    }
}
