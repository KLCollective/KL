using KinkLinkCommon.Domain.Enums;

namespace KinkLinkCommon.Domain.Network.LoginAuthentication;

public record LoginAuthenticationResult(LoginAuthenticationErrorCode ErrorCode, string Token);

public record ListProfiles(LoginAuthenticationErrorCode ErrorCode, string[] Profiles);
