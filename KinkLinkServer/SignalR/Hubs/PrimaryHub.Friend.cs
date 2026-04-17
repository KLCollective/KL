using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.AddFriend;
using KinkLinkCommon.Domain.Network.RemoveFriend;
using KinkLinkCommon.Domain.Network.UpdateFriend;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.AddFriend)]
    public async Task<AddFriendResponse> AddFriend(AddFriendRequest request)
    {
        var friendCode = FriendCode;
        logger.LogInformation("[SignalR] AddFriend: {FriendCode} -> {Target}", friendCode, request.TargetFriendCode);
        LogWithBehavior($"[AddFriendRequest] Sender = {friendCode}, Target = {request.TargetFriendCode}", LogMode.Both);
        return await addFriendHandler.Handle(friendCode, request, Clients);
    }

    [HubMethodName(HubMethod.RemoveFriend)]
    public async Task<RemovePair> RemoveFriend(RemoveFriendRequest request)
    {
        var friendCode = FriendCode;
        logger.LogInformation("[SignalR] RemoveFriend: {FriendCode} -> {Target}", friendCode, request.TargetFriendCode);
        LogWithBehavior($"[RemoveFriendRequest] Sender = {friendCode}, Target = {request.TargetFriendCode}", LogMode.Both);
        return await removeFriendHandler.Handle(friendCode, request, Clients);
    }

    [HubMethodName(HubMethod.UpdateFriend)]
    public async Task<UpdateFriendResponse> UpdateFriend(UpdateFriendRequest request)
    {
        var friendCode = FriendCode;
        logger.LogInformation("[SignalR] UpdateFriend: {FriendCode} -> {Target}, Perms: {Perms}", friendCode, request.TargetFriendCode, request.Permissions);
        LogWithBehavior($"[UpdateFriendRequest] Sender = {friendCode}, Target = {request.TargetFriendCode}, Permissions = {request.Permissions}", LogMode.Disk);
        return await updateFriendHandler.Handle(friendCode, request, Clients);
    }
}
