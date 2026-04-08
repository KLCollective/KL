using Discord.WebSocket;
using KinkLinkBot.Configuration;
using KinkLinkBot.Domain.Models;
using KinkLinkCommon.Database;
using KinkLinkCommon.Security;
using Microsoft.Extensions.Logging;

namespace KinkLinkBot.Services;

/// <summary>
///     Service for managing user registrations using direct PostgreSQL access
///     These are all handled by a series of embeded messages that only the users
///     can see. There is no need for any slash commands to be used.
/// </summary>
public class RegistrationService
{
    private readonly AuthSql _auth;
    private readonly UsersSql _users;
    private readonly ProfilesSql _profiles;
    private readonly ILogger<RegistrationService> _logger;
    private readonly DiscordSocketClient _client;
    private readonly BotConfiguration _config;
    private readonly ISecretHasher _secretHasher;

    public RegistrationService(
        ILogger<RegistrationService> logger,
        DiscordSocketClient client,
        BotConfiguration config,
        ISecretHasher secretHasher
    )
    {
        var connectionString = config.DbConnectionString;
        _auth = new AuthSql(connectionString);
        _users = new UsersSql(connectionString);
        _profiles = new ProfilesSql(connectionString);
        _secretHasher = secretHasher;
        _logger = logger;
        _client = client;
        _config = config;
    }

    /// <summary>
    ///     Gets a user by Discord ID
    /// </summary>
    public async Task<User?> GetUserByDiscordIdAsync(ulong discordId)
    {
        try
        {
            var userRow = await _users.SelectUserbyDiscordIdAsync(new((long)discordId));

            if (userRow.HasValue)
            {
                return new User(
                    Id: userRow.Value.Id,
                    DiscordId: userRow.Value.DiscordId,
                    SecretKeyHash: userRow.Value.SecretKeyHash,
                    Verified: userRow.Value.Verified,
                    Banned: userRow.Value.Banned,
                    CreatedAt: userRow.Value.CreatedAt,
                    UpdatedAt: userRow.Value.UpdatedAt
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by Discord ID {DiscordId}", discordId);
            return null;
        }
    }

    /// <summary>
    ///     Gets all profiles for a user
    /// </summary>
    public async Task<List<Profile>> GetUserProfilesAsync(ulong discordId)
    {
        try
        {
            var user = await GetUserByDiscordIdAsync(discordId);
            if (!user.HasValue)
                return new List<Profile>();

            var profileRows = await _profiles.ListUIDsForUserAsync(new(user.Value.Id));
            var profiles = new List<Profile>();

            foreach (var row in profileRows)
            {
                profiles.Add(
                    new Profile(
                        Id: 0, // We don't have the ID from ListUIDsForUser, but we don't need it for display
                        UserId: user.Value.Id,
                        Uid: row.Uid,
                        ChatRole: null,
                        Alias: null,
                        Title: null,
                        Description: null,
                        CreatedAt: null,
                        UpdatedAt: null
                    )
                );
            }

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for Discord ID {DiscordId}", discordId);
            return new List<Profile>();
        }
    }

    /// <summary>
    ///     Registers a new user account or returns existing if already registered
    /// </summary>
    public async Task<RegistrationResponse> RegisterUserAccount(ulong discordId)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _users.SelectUserbyDiscordIdAsync(new((long)discordId));

            if (existingUser.HasValue)
            {
                // User already exists, check if they're banned
                if (existingUser.Value.Banned == true)
                {
                    return new RegistrationResponse
                    {
                        Success = false,
                        ErrorMessage =
                            "Your account has been banned. Please contact an administrator.",
                    };
                }

                // Return existing user info - since we're not migrating existing data,
                // this should not happen for production use
                _logger.LogWarning(
                    "Attempted to retrieve existing user with legacy plaintext secret during migration"
                );
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage =
                        "Account already exists. Please contact an administrator for secret recovery.",
                };
            }

            // Create new user
            var secret = GenerateSecret();
            var newUser = await _users.RegisterNewUserAsync(
                new(DiscordId: (long)discordId, SecretKey: secret)
            );

            if (newUser.HasValue)
            {
                _logger.LogInformation(
                    "New user registered: Discord ID {DiscordUserId}, User ID {UserId}",
                    discordId,
                    newUser.Value.Id
                );

                return new RegistrationResponse
                {
                    Success = true,
                    Secret = secret, // Return the original plaintext secret for user to save
                };
            }
            else
            {
                _logger.LogError(
                    "Failed to register new user with Discord ID {DiscordUserId}",
                    discordId
                );
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to create user account. Please try again later.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error registering user account for Discord ID {DiscordId}",
                discordId
            );
            return new RegistrationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during registration.",
            };
        }
    }

    /// <summary>
    ///     Removes a user's account
    /// </summary>
    public async Task<RegistrationResponse> RemoveAccount(ulong discordId)
    {
        try
        {
            var existingUser = await _users.SelectUserbyDiscordIdAsync(new((long)discordId));

            if (!existingUser.HasValue)
            {
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "User account not found.",
                };
            }

            var deletedUser = await _users.DeleteUserAccountAsync(new((long)discordId));

            if (deletedUser.HasValue)
            {
                _logger.LogInformation(
                    "User account deleted: Discord ID {DiscordUserId}, User ID {UserId}",
                    discordId,
                    deletedUser.Value.Id
                );

                return new RegistrationResponse { Success = true, ErrorMessage = null };
            }
            else
            {
                _logger.LogError(
                    "Failed to delete user account with Discord ID {DiscordUserId}",
                    discordId
                );
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to delete user account. Please try again later.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error removing user account for Discord ID {DiscordId}",
                discordId
            );
            return new RegistrationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while deleting your account.",
            };
        }
    }

    /// <summary>
    ///     Creates a UID for the  user.
    ///     UIDs are used to maintain relative anonymity with the user accounts.
    /// </summary>
    public async Task<ProfileResponse> CreateUID(ulong discordId, string? lastFour = null)
    {
        try
        {
            var existingUser = await _users.SelectUserbyDiscordIdAsync(new((long)discordId));

            if (!existingUser.HasValue)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "User account not found. Please register first.",
                };
            }

            if (existingUser.Value.Banned == true)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "Your account has been banned. Please contact an administrator.",
                };
            }

            var newUID = GenerateUID(lastFour);

            var profileExists = await _profiles.ProfileExistsAsync(new(newUID));
            // This is _exceedingly unlikely_ but I'm paranoid, so just in case.
            // (Odds of this occurring are 1/3,656,158,440,062,976)
            if (profileExists?.Exists == true)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage =
                        "We currently have Unable to generate a unique UID. Please try again later.",
                };
            }

            var newProfile = await _profiles.CreateNewUIDForUserAsync(
                new(
                    UserId: existingUser.Value.Id,
                    Uid: newUID,
                    // TODO: Clean this up (allow for alias to be set during creation)
                    ChatRole: null,
                    Alias: null,
                    Title: null,
                    Description: null
                )
            );

            if (newProfile.HasValue)
            {
                _logger.LogInformation(
                    "New UID created: {ProfileUID} for Discord ID {DiscordUserId}, User ID {UserId}",
                    newUID,
                    discordId,
                    existingUser.Value.Id
                );

                return new ProfileResponse { Success = true, UID = newUID };
            }
            else
            {
                _logger.LogError(
                    "Failed to create UID {ProfileUID} for Discord ID {DiscordUserId}",
                    newUID,
                    discordId
                );
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to create UID. Please try again later.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating UID for Discord ID {DiscordId}", discordId);
            return new ProfileResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while creating your UID.",
            };
        }
    }

    public async Task<ProfileResponse> DeleteUID(ulong discordId, string UID)
    {
        try
        {
            var existingUser = await _users.SelectUserbyDiscordIdAsync(new((long)discordId));

            if (!existingUser.HasValue)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "User account not found.",
                };
            }

            var profileExists = await _profiles.ProfileExistsAsync(new(UID));
            if (!profileExists?.Exists == true)
            {
                return new ProfileResponse { Success = false, ErrorMessage = "UID not found." };
            }

            var deletedProfile = await _profiles.DeleteProfileAsync(
                new(UID, existingUser.Value.Id)
            );

            if (deletedProfile.HasValue)
            {
                _logger.LogInformation(
                    "UID {ProfileUID} deleted for Discord ID {DiscordUserId}, User ID {UserId}",
                    UID,
                    discordId,
                    existingUser.Value.Id
                );

                return new ProfileResponse { Success = true, ErrorMessage = null };
            }
            else
            {
                _logger.LogError(
                    "Failed to delete UID {ProfileUID} for Discord ID {DiscordUserId}",
                    UID,
                    discordId
                );
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to delete UID. The UID may not belong to your account.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting UID {UID} for Discord ID {DiscordId}",
                UID,
                discordId
            );
            return new ProfileResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while deleting your UID.",
            };
        }
    }

    /// <summary>
    ///     Creates a new profile with an optional alias for the user
    /// </summary>
    public async Task<ProfileResponse> CreateProfileWithAliasAsync(
        ulong discordId,
        string? alias,
        string? lastFour = null
    )
    {
        try
        {
            var existingUser = await GetUserByDiscordIdAsync(discordId);

            if (!existingUser.HasValue)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "User account not found. Please register first.",
                };
            }

            if (existingUser.Value.Banned == true)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "Your account has been banned. Please contact an administrator.",
                };
            }

            // Check profile limit (10 profiles max)
            var existingProfiles = await GetUserProfilesAsync(discordId);
            if (existingProfiles.Count >= 10)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage =
                        "You have reached the maximum limit of 10 profiles. Please delete an existing profile to create a new one.",
                };
            }

            var newUID = GenerateUID(lastFour);
            var profileExists = await _profiles.ProfileExistsAsync(new(newUID));

            // This is exceedingly unlikely but I'm paranoid, so just in case.
            // (Odds of this occurring are 1/3,656,158,440,062,976)
            if (profileExists?.Exists == true)
            {
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "Unable to generate a unique UID. Please try again later.",
                };
            }

            var newProfile = await _profiles.CreateNewUIDForUserAsync(
                new(
                    UserId: existingUser.Value.Id,
                    Uid: newUID,
                    ChatRole: null,
                    Alias: string.IsNullOrEmpty(alias) ? null : alias,
                    Title: null,
                    Description: null
                )
            );

            if (newProfile.HasValue)
            {
                _logger.LogInformation(
                    "New profile created: {ProfileUID} for Discord ID {DiscordUserId}, User ID {UserId}",
                    newUID,
                    discordId,
                    existingUser.Value.Id
                );

                return new ProfileResponse { Success = true, UID = newUID };
            }
            else
            {
                _logger.LogError(
                    "Failed to create profile {ProfileUID} for Discord ID {DiscordUserId}",
                    newUID,
                    discordId
                );
                return new ProfileResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to create profile. Please try again later.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile for Discord ID {DiscordId}", discordId);
            return new ProfileResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while creating your profile.",
            };
        }
    }

    /// <summary>
    ///     Regenerates a user's secret key
    /// </summary>
    public async Task<RegistrationResponse> RegenerateSecretAsync(ulong discordId)
    {
        try
        {
            var existingUser = await GetUserByDiscordIdAsync(discordId);

            if (!existingUser.HasValue)
            {
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "User account not found.",
                };
            }

            if (existingUser.Value.Banned == true)
            {
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Your account has been banned. Please contact an administrator.",
                };
            }

            // Generate new secret and hash it
            var newSecret = GenerateSecret();

            // Update the user's secret in the database
            await _users.RegenerateSecretKeyAsync(
                new(DiscordId: (long)discordId, SecretKey: newSecret)
            );

            _logger.LogInformation(
                "Secret regenerated for Discord ID {DiscordUserId}, User ID {UserId}",
                discordId,
                existingUser.Value.Id
            );

            return new RegistrationResponse
            {
                Success = true,
                Secret = newSecret, // Return the new plaintext secret for user to save
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating secret for Discord ID {DiscordId}", discordId);
            return new RegistrationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while regenerating your secret.",
            };
        }
    }

    private static string GenerateUID(string? customSuffix = null)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        if (!string.IsNullOrEmpty(customSuffix) && customSuffix.Length == 4)
        {
            var prefix = new char[6];
            for (int i = 0; i < 6; i++)
            {
                prefix[i] = chars[random.Next(chars.Length)];
            }
            return new string(prefix) + customSuffix.ToUpper();
        }

        var result = new char[10];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
    }

    private static string GenerateSecret()
    {
        // Generate a secure random 64-character secret
        return Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)
        );
    }
}
