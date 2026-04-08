using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.GetProfiles;
using KinkLinkCommon.Domain.Network.GetToken;
using KinkLinkCommon.Domain.Network.LoginAuthentication;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace KinkLinkServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ILogger<AuthController> logger,
    Configuration config,
    AuthService authService,
    IMetricsService metricsService
) : ControllerBase
{
    // Const
    private static readonly Version ExpectedVersion = new(0, 0, 0, 1);

    // Instantiated
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(config.SigningKey));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] GetTokenRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = "login";

        logger.LogInformation(
            "Login request received for profile UID {ProfileUID}, version {Version}",
            request.ProfileUID,
            request.Version
        );

        if (request.Version < ExpectedVersion)
        {
            metricsService.IncrementAuthentication(actionName, false);
            metricsService.RecordAuthenticationDuration(actionName, stopwatch.ElapsedMilliseconds);
            logger.LogWarning(
                "Version mismatch for profile {ProfileUID}: expected {ExpectedVersion}, got {ActualVersion}",
                request.ProfileUID,
                ExpectedVersion,
                request.Version
            );
            return StatusCode(
                StatusCodes.Status409Conflict,
                new LoginAuthenticationResult(LoginAuthenticationErrorCode.VersionMismatch, "")
            );
        }

        var authStatus = await authService.LoginUser(request.Secret, request.ProfileUID);
        if (authStatus != DBAuthenticationStatus.Authorized)
        {
            metricsService.IncrementAuthentication(actionName, false);
            metricsService.RecordAuthenticationDuration(actionName, stopwatch.ElapsedMilliseconds);
            logger.LogWarning(
                "Login failed for profile {ProfileUID}: {Status}",
                request.ProfileUID,
                authStatus
            );
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                new LoginAuthenticationResult(LoginAuthenticationErrorCode.UnknownSecret, "")
            );
        }

        var token = GenerateJwtToken([new Claim(AuthClaimTypes.Uid, request.ProfileUID)]);
        metricsService.IncrementAuthentication(actionName, true);
        metricsService.RecordAuthenticationDuration(actionName, stopwatch.ElapsedMilliseconds);
        logger.LogInformation("Login successful for profile {ProfileUID}", request.ProfileUID);
        return StatusCode(
            StatusCodes.Status200OK,
            new LoginAuthenticationResult(LoginAuthenticationErrorCode.Success, token.RawData)
        );
    }

    private JwtSecurityToken GenerateJwtToken(List<Claim> claims)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = "jwt_generation";

        logger.LogInformation("Generating JWT token with {ClaimCount} claims", claims.Count);

        var token = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                _key,
                SecurityAlgorithms.HmacSha256Signature
            ),
            Expires = DateTime.UtcNow.AddHours(4),
        };

        var jwtToken = new JwtSecurityTokenHandler().CreateJwtSecurityToken(token);
        stopwatch.Stop();

        metricsService.IncrementAuthentication(actionName, true);
        metricsService.RecordAuthenticationDuration(actionName, stopwatch.ElapsedMilliseconds);
        logger.LogInformation(
            "JWT token generated successfully, expires at {ExpiryTime}",
            token.Expires
        );
        return jwtToken;
    }

    [AllowAnonymous]
    [HttpPost("profiles")]
    public async Task<IActionResult> GetProfiles([FromBody] GetProfilesRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = "get_profiles";

        logger.LogInformation("GetProfiles request received for secret");

        try
        {
            var profiles = await authService.GetProfilesForKey(request.Secret);
            metricsService.IncrementAuthentication(actionName, true);
            metricsService.RecordAuthenticationDuration(actionName, stopwatch.ElapsedMilliseconds);
            logger.LogInformation("GetProfiles successful, returned {Count} profiles", profiles.Count);
            var profileInfos = profiles.Select(p => new ProfileInfo(p.Item1, p.Item2)).ToList();
            return Ok(new GetProfilesResult(profileInfos));
        }
        catch (Exception ex)
        {
            metricsService.IncrementAuthentication(actionName, false);
            metricsService.RecordAuthenticationDuration(actionName, stopwatch.ElapsedMilliseconds);
            logger.LogError(ex, "GetProfiles failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new GetProfilesResult([]));
        }
    }
}
