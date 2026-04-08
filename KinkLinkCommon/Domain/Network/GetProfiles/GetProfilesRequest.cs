namespace KinkLinkCommon.Domain.Network.GetProfiles;

public record GetProfilesRequest(string Secret);

public record ProfileInfo(string Uid, string Alias);

public record GetProfilesResult(List<ProfileInfo> Profiles);