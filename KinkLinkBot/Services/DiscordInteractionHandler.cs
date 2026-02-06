using Microsoft.Extensions.Logging;
using KinkLinkBot.Configuration;
using KinkLinkBot.Domain.Models;
using Discord;
using Discord.WebSocket;

namespace KinkLinkBot.Services;

/// <summary>
///     Handles Discord button interactions for registration
/// </summary>
public class DiscordInteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly RegistrationService _registrationService;
    private readonly ulong _adminRoleId;
    private readonly ulong _guildId;
    private readonly ulong _channelId;
    private readonly ILogger<DiscordInteractionHandler> _logger;

    public DiscordInteractionHandler(
        DiscordSocketClient client,
        RegistrationService registrationService,
        BotConfiguration config,
        ILogger<DiscordInteractionHandler> logger)
    {
        _client = client;
        _registrationService = registrationService;
        _adminRoleId = config.Bot.AdminRoleId;
        _channelId = config.Bot.ChannelId;
        _guildId = config.Bot.GuildId;
        _logger = logger;
    }

    public void Initialize()
    {
        _client.ButtonExecuted += HandleButtonInteractionAsync;
        _client.ModalSubmitted += HandleModalSubmissionAsync;
        _client.Ready += EnsureRegistrationPromptExistsAsync;
    }

    public async Task EnsureRegistrationPromptExistsAsync()
    {
        try
        {
            var guild = _client.GetGuild(_guildId);
            if (guild == null)
            {
                _logger.LogError("Guild {GuildId} not found during startup", _guildId);
                return;
            }

            var channel = guild.GetTextChannel(_channelId);
            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found in guild {GuildId} during startup", _channelId, _guildId);
                return;
            }

            var existingPinnedMessages = await channel.GetPinnedMessagesAsync();
            var hasRegistrationPrompt = existingPinnedMessages.Any(IsRegistrationPromptMessage);

            if (!hasRegistrationPrompt)
            {
                _logger.LogInformation("No registration prompt found in channel {ChannelId}, creating new one", channel.Id);
                await CreatePrompt();
            }
            else
            {
                _logger.LogInformation("Registration prompt already exists in channel {ChannelId}", channel.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring registration prompt exists during startup");
        }
    }

    private bool IsRegistrationPromptMessage(IMessage message)
    {
        return message.Embeds?.Any(embed =>
            embed.Title?.Contains("kinklink account management", StringComparison.OrdinalIgnoreCase) == true) == true;
    }


    public async Task<RegistrationResponse> CreatePrompt()
    {
        try
        {
            // Get the guild from the client
            var guild = _client.GetGuild(_guildId);
            if (guild == null)
            {
                _logger.LogError("Guild {GuildId} not found", _guildId);
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Guild not found"
                };
            }

            // Get the specific channel from configuration
            var channel = guild.GetTextChannel(_channelId);
            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found in guild {GuildId}", _channelId, _guildId);
                return new RegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Registration channel not found"
                };
            }

            // Create an embed for the registration prompt
            var embed = new EmbedBuilder
            {
                Title = "🔗 KinkLink Account Management",
                Description = "Welcome to KinkLink! To get started, please click the button below to create your account",
                Color = Color.Blue,
            };

            embed.AddField("❓ Need Help?",
                "Contact an administrator if you encounter issues with registration",
                inline: false);

            embed.WithFooter("KinkLink Bot • Account Management");
            embed.WithCurrentTimestamp();

            // Create a button for user interaction
            var buttonBuilder = new ComponentBuilder()
                .WithButton("Start account management", "register_start", ButtonStyle.Primary);

            // Send the embed message to the channel
            var message = await channel.SendMessageAsync(
                embed: embed.Build(),
                components: buttonBuilder.Build());

            try
            {
                await message.PinAsync();
                _logger.LogInformation("Registration prompt sent and pinned to channel {ChannelId} in guild {GuildId}", channel.Id, guild.Id);
            }
            catch (Exception pinEx)
            {
                _logger.LogWarning(pinEx, "Failed to pin registration message to channel {ChannelId} in guild {GuildId}", channel.Id, guild.Id);
            }

            return new RegistrationResponse
            {
                Success = true,
                ErrorMessage = null,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating registration prompt");
            return new RegistrationResponse
            {
                Success = false,
                ErrorMessage = $"Failed to create registration prompt: {ex.Message}"
            };
        }
    }
    private async Task HandleButtonInteractionAsync(SocketMessageComponent component)
    {
        // Only handle interactions in the configured guild
        if (component.GuildId != _guildId)
            return;

        switch (component.Data.CustomId)
        {
            case "register_start":
                await HandleRegistrationStartAsync(component);
                break;
            case "register_confirmed":
                var user = component.User as SocketGuildUser;
                if (user == null)
                {
                    await component.RespondAsync("Error: Unable to identify user.", ephemeral: true);
                    return;
                }
                await HandleNewUserRegistration(component, user);
                break;
            case "create_first_uid":
                await HandleCreateUIDAsync(component);
                break;
            case "skip_uid_creation":
                await HandleSkipUIDCreationAsync(component);
                break;
            case "remove_account":
                await HandleAccountRemovalAsync(component);
                break;
            case "create_uid":
                await HandleCreateUIDAsync(component);
                break;
            case "remove_uid":
                await HandleRemoveUIDAsync(component);
                break;
            case "create_profile":
                await HandleCreateProfileAsync(component);
                break;
            case "delete_profile_prompt":
                await HandleDeleteProfilePromptAsync(component);
                break;
            case "regenerate_secret_prompt":
                await HandleRegenerateSecretPromptAsync(component);
                break;
            case "delete_account_prompt":
                await HandleDeleteAccountPromptAsync(component);
                break;
        }
    }

    private async Task HandleRegistrationStartAsync(SocketMessageComponent component)
    {
        try
        {
            var user = component.User as SocketGuildUser;
            if (user == null)
            {
                await component.RespondAsync("Error: Unable to identify user.", ephemeral: true);
                return;
            }

            var existingUser = await _registrationService.GetUserByDiscordIdAsync(user.Id);
            // Registered users can skip to the account management while unregistered will need to create an account first.
            if (existingUser.HasValue)
            {
                await ShowAccountManagementAsync(component, user, existingUser.Value);
            }
            else
            {
                await HandleNewUserConfirmation(component, user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling registration start for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }
private async Task HandleNewUserConfirmation(SocketMessageComponent component, SocketGuildUser user) {
                var embed = new EmbedBuilder
            {
                Title = "🔗 KinkLink Registration",
                Description = "Welcome to KinkLink! To get started, please register your account.",
                Color = Color.Blue,
            };

            embed.AddField("📝 Registration Steps",
                "1. Click the button below to start registration\n" +
                "2. Follow the prompts to create your unique secret key and first UID\n" +
                "3. Use the provided secret key to connect with KinkLink in game",
                inline: false);

            embed.AddField("📋 Community Rules",
                "• **18+ Player Only**: This is a legal thing, minors cannot consent, therefore they are not allowed\n" +
                "• **18+ Characters Only**: This is a values thing, even if you are 18+ IRL, don't use child avatars with this service\n" +
                "• **Safe Space**: Zero tolerance for harassment, bullying, or stalking behavior\n" +
                "• **Understand Consent**: The core of BDSM is consent, be sure you understand what it means for you and your partner(s). Respect boundaries and limits\n" +
                "• **Be Respectful**: We're all people, do give common courtesy and respect",
                inline: false);

            embed.AddField("🔐 Privacy & Security",
                "• Your Discord ID number (not your username or anything like that) will be stored on the server for *account management purposes only*.\n" +
                "• Profiles (noted by UIDs) provide anonymity in-game\n" +
                "• You can delete your account at any time",
                inline: false);
            embed.WithCurrentTimestamp();

            var buttonBuilder = new ComponentBuilder()
                .WithButton("I understand, I wish to create my account", "register_confirmed", ButtonStyle.Primary);

            await component.RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);

}
    private async Task HandleNewUserRegistration(SocketMessageComponent component, SocketGuildUser user)
    {
        var response = await _registrationService.RegisterUserAccount(user.Id);

        if (response.Success)
        {
            var embed = new EmbedBuilder
            {
                Title = "✅ Registration Successful",
                Description = $"Your KinkLink account has been created!\n\n **Your Secret is:** `{response.Secret}`\n\n Be sure to save it, we cannot return it to you.",
                Color = Color.Green,
                Footer = new EmbedFooterBuilder { Text = "Keep your secret safe!" }
            };

            embed.AddField("🎯 Create Your First UID?",
                "A UID is your public identity that others use to connect with you. You can create one now or skip and create it later.",
                inline: false);

            embed.AddField("⚠️ Important",
                "• Your UID is public and used to connect with others\n" +
                "• Your secret is private and authenticates your connection\n" +
                "• Never share your secret with anyone!",
                inline: false);

            var buttonBuilder = new ComponentBuilder()
                .WithButton("🎭 Create First UID", "create_first_uid", ButtonStyle.Primary)
                .WithButton("⏭️ Skip for Now", "skip_uid_creation", ButtonStyle.Secondary);

            await component.RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
            _logger.LogInformation("User {Username} ({UserId}) successfully registered Discord account", user.Username, user.Id);
        }
        else
        {
            var errorEmbed = new EmbedBuilder
            {
                Title = "❌ Registration Failed",
                Description = response.ErrorMessage ?? "An unknown error occurred",
                Color = Color.Red
            };

            await component.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
            _logger.LogWarning("Registration failed for user {Username} ({UserId}): {ErrorMessage}", user.Username, user.Id, response.ErrorMessage);
        }
    }

    private async Task ShowAccountManagementAsync(SocketMessageComponent component, SocketGuildUser user, KinkLinkCommon.Database.User userRecord)
    {
        try
        {
            var profiles = await _registrationService.GetUserProfilesAsync(user.Id);

            var embed = new EmbedBuilder
            {
                Title = "🔧 Account Management",
                Description = $"Welcome {user.Username}! Manage your KinkLink account settings and profiles.",
                Color = Color.Blue
            };

            if (profiles.Any())
            {
                var profileList = string.Join("\n", profiles.Select(p => $"• **{p.Uid}**{(string.IsNullOrEmpty(p.Alias) ? "" : $" - {p.Alias}")}"));
                embed.AddField("📋 Your Profiles", profileList, inline: false);
            }
            else
            {
                embed.AddField("📋 Your Profiles", "No profiles created yet. Create your first profile to get started!", inline: false);
            }

            embed.AddField("📊 Account Status",
                $"• Account created: {userRecord.CreatedAt:yyyy-MM-dd}\n" +
                $"• Total profiles: {profiles.Count}",
                inline: false);

            embed.WithFooter("KinkLink Bot • Account Management");
            embed.WithCurrentTimestamp();

            var buttonBuilder = new ComponentBuilder()
                .WithButton("➕ Create Profile", "create_profile", ButtonStyle.Primary)
                .WithButton("🗑️ Delete Profile", "delete_profile_prompt", ButtonStyle.Secondary)
                .WithButton("🔑 Regenerate Secret", "regenerate_secret_prompt", ButtonStyle.Secondary)
                .WithButton("❌ Delete Account", "delete_account_prompt", ButtonStyle.Danger);

            await component.RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing account management for user {UserId}", user.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleAccountRemovalAsync(SocketMessageComponent component)
    {
        try
        {
            var modal = new ModalBuilder()
                .WithTitle("Confirm Account Deletion")
                .WithCustomId("confirm_account_removal")
                .AddTextInput("Type 'DELETE' to confirm", "confirmation", TextInputStyle.Short,
                    placeholder: "This action is irreversible!", required: true);

            await component.RespondWithModalAsync(modal.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling account removal prompt for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleCreateUIDAsync(SocketMessageComponent component)
    {
        try
        {
            var response = await _registrationService.CreateUID(component.User.Id);

            if (response.Success)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("✅ UID Created Successfully")
                    .WithDescription($"Your new UID has been generated. Use this UID to connect to the game client.\n\n**UID:** `{response.UID}`")
                    .WithColor(Color.Green)
                    .AddField("Next Steps", "1. Open KinkLink plugin in FFXIV\n2. Enter your UID and secret key\n3. Configure your profile settings")
                    .WithFooter("Keep your secret key secure!")
                    .Build();

                await component.RespondAsync(embed: embed, ephemeral: true);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithTitle("❌ UID Creation Failed")
                    .WithDescription(response.ErrorMessage ?? "Unknown error occurred")
                    .WithColor(Color.Red)
                    .Build();

                await component.RespondAsync(embed: embed, ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UID creation for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred while creating your UID. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleSkipUIDCreationAsync(SocketMessageComponent component)
    {
        try
        {
            var embed = new EmbedBuilder
            {
                Title = "✅ Registration Complete",
                Description = "You can create your UID later through the bot commands or by registering again.",
                Color = Color.Green
            };

            embed.AddField("🔗 Next Steps",
                "1. Open KinkLink plugin in FFXIV\n" +
                "2. Enter your Secret when prompted\n" +
                "3. Create your UID when you're ready",
                inline: false
            );

            embed.AddField("💡 Tip",
                "You can create multiple UIDs later for different characters, roles, or OC.",
                inline: false
            );

            await component.RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling skip UID creation for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleRemoveUIDAsync(SocketMessageComponent component)
    {
        try
        {
            var modal = new ModalBuilder()
                .WithTitle("Remove UID")
                .WithCustomId("confirm_uid_removal")
                .AddTextInput("Enter UID to remove", "uid", TextInputStyle.Short,
                    placeholder: "Enter the UID you want to delete", required: true)
                .AddTextInput("Type 'DELETE' to confirm", "confirmation", TextInputStyle.Short,
                    placeholder: "This action is irreversible!", required: true);

            await component.RespondWithModalAsync(modal.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UID removal prompt for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleModalSubmissionAsync(SocketModal modal)
    {
        if (modal.GuildId != _guildId)
            return;

        switch (modal.Data.CustomId)
        {
            case "confirm_account_removal":
                await HandleAccountRemovalConfirmationAsync(modal);
                break;
            case "confirm_uid_removal":
                await HandleUIDRemovalConfirmationAsync(modal);
                break;
            case "create_profile_modal":
                await HandleCreateProfileModalAsync(modal);
                break;
            case "delete_profile_modal":
                await HandleDeleteProfileModalAsync(modal);
                break;
            case "regenerate_secret_modal":
                await HandleRegenerateSecretModalAsync(modal);
                break;
            case "delete_account_modal":
                await HandleDeleteAccountModalAsync(modal);
                break;
        }
    }

    private async Task HandleAccountRemovalConfirmationAsync(SocketModal modal)
    {
        try
        {
            var confirmation = modal.Data.Components.FirstOrDefault(x => x.CustomId == "confirmation")?.Value;

            if (confirmation?.Trim().ToUpper() != "DELETE")
            {
                await modal.RespondAsync("Confirmation text incorrect. Account deletion cancelled.", ephemeral: true);
                return;
            }

            var response = await _registrationService.RemoveAccount(modal.User.Id);

            if (response.Success)
            {
                var embed = new EmbedBuilder
                {
                    Title = "✅ Account Deleted",
                    Description = "Your KinkLink account and all associated data have been permanently deleted.",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: embed.Build(), ephemeral: true);
                _logger.LogInformation("User {Username} ({UserId}) successfully deleted their account", modal.User.Username, modal.User.Id);
            }
            else
            {
                var errorEmbed = new EmbedBuilder
                {
                    Title = "❌ Account Deletion Failed",
                    Description = response.ErrorMessage ?? "An unknown error occurred",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                _logger.LogWarning("Account deletion failed for user {Username} ({UserId}): {ErrorMessage}", modal.User.Username, modal.User.Id, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling account removal confirmation for user {UserId}", modal.User.Id);
            await modal.RespondAsync("An unexpected error occurred while deleting your account. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleUIDRemovalConfirmationAsync(SocketModal modal)
    {
        try
        {
            var uid = modal.Data.Components.FirstOrDefault(x => x.CustomId == "uid")?.Value?.Trim();
            var confirmation = modal.Data.Components.FirstOrDefault(x => x.CustomId == "confirmation")?.Value?.Trim().ToUpper();

            if (confirmation != "DELETE")
            {
                await modal.RespondAsync("Confirmation text incorrect. UID deletion cancelled.", ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(uid))
            {
                await modal.RespondAsync("UID is required.", ephemeral: true);
                return;
            }

            var response = await _registrationService.DeleteUID(modal.User.Id, uid);

            if (response.Success)
            {
                var embed = new EmbedBuilder
                {
                    Title = "✅ UID Deleted",
                    Description = $"UID `{uid}` has been permanently deleted.",
                    Color = Color.Green
                };

                await modal.RespondAsync(embed: embed.Build(), ephemeral: true);
                _logger.LogInformation("User {Username} ({UserId}) successfully deleted UID {Uid}", modal.User.Username, modal.User.Id, uid);
            }
            else
            {
                var errorEmbed = new EmbedBuilder
                {
                    Title = "❌ UID Deletion Failed",
                    Description = response.ErrorMessage ?? "An unknown error occurred",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                _logger.LogWarning("UID deletion failed for user {Username} ({UserId}), UID {Uid}: {ErrorMessage}", modal.User.Username, modal.User.Id, uid, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UID removal confirmation for user {UserId}", modal.User.Id);
            await modal.RespondAsync("An unexpected error occurred while deleting your UID. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleCreateProfileAsync(SocketMessageComponent component)
    {
        try
        {
            var modal = new ModalBuilder()
                .WithTitle("Create Profile")
                .WithCustomId("create_profile_modal")
                .AddTextInput("What would you like your profile alias to be?", "alias", TextInputStyle.Short,
                    placeholder: "Optional - leave blank for no alias", required: false, maxLength: 20);

            await component.RespondWithModalAsync(modal.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling create profile prompt for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleDeleteProfilePromptAsync(SocketMessageComponent component)
    {
        try
        {
            var user = component.User as SocketGuildUser;
            if (user == null)
            {
                await component.RespondAsync("Error: Unable to identify user.", ephemeral: true);
                return;
            }

            var profiles = await _registrationService.GetUserProfilesAsync(user.Id);

            if (!profiles.Any())
            {
                var embed = new EmbedBuilder
                {
                    Title = "❌ No Profiles Found",
                    Description = "You don't have any profiles to delete.",
                    Color = Color.Red
                };
                await component.RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var modal = new ModalBuilder()
                .WithTitle("Delete Profile")
                .WithCustomId("delete_profile_modal")
                .AddTextInput("Select profile to delete", "profile_uid", TextInputStyle.Short,
                    placeholder: $"Available profiles: {string.Join(", ", profiles.Select(p => p.Uid))}", required: true)
                .AddTextInput("Type 'DELETE' to confirm", "confirmation", TextInputStyle.Short,
                    placeholder: "This action is irreversible!", required: true);

            await component.RespondWithModalAsync(modal.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete profile prompt for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleRegenerateSecretPromptAsync(SocketMessageComponent component)
    {
        try
        {
            var modal = new ModalBuilder()
                .WithTitle("Regenerate secret?")
                .WithCustomId("regenerate_secret_modal")
                .AddTextInput("Type 'CONFIRM' to regenerate your secret", "confirmation", TextInputStyle.Short,
                    placeholder: "Warning: This will invalidate all active sessions!", required: true);

            await component.RespondWithModalAsync(modal.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling regenerate secret prompt for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleDeleteAccountPromptAsync(SocketMessageComponent component)
    {
        try
        {
            var modal = new ModalBuilder()
                .WithTitle("Are you sure you wish to delete your account?")
                .WithCustomId("delete_account_modal")
                .AddTextInput("Type 'YES I WANT TO DELETE' to confirm", "confirmation", TextInputStyle.Short,
                    placeholder: "This action is irreversible and will delete all your data!", required: true);

            await component.RespondWithModalAsync(modal.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete account prompt for user {UserId}", component.User.Id);
            await component.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleCreateProfileModalAsync(SocketModal modal)
    {
        try
        {
            var alias = modal.Data.Components.FirstOrDefault(x => x.CustomId == "alias")?.Value?.Trim();


            if (!string.IsNullOrEmpty(alias) && alias.Length > 20)
            {
                await modal.RespondAsync("Alias is too long. Maximum 20 characters allowed.", ephemeral: true);
                return;
            }

            var response = await _registrationService.CreateProfileWithAliasAsync(modal.User.Id, alias);

            if (response.Success)
            {
                var embed = new EmbedBuilder
                {
                    Title = "✅ Profile Created Successfully",
                    Description = $"Your new profile has been created!\n\n**UID:** `{response.UID}`",
                    Color = Color.Green
                };

                if (!string.IsNullOrEmpty(alias))
                {
                    embed.AddField("Alias", alias, inline: true);
                }

                embed.AddField("Next Steps", "You can now use this UID in the game client to connect with others.");
                embed.WithFooter("KinkLink Bot • Profile Management");

                var buttonBuilder = new ComponentBuilder()
                    .WithButton("🔧 Back to Account Management", "register_start", ButtonStyle.Secondary);

                await modal.RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
                _logger.LogInformation("User {Username} ({UserId}) created new profile {Uid}", modal.User.Username, modal.User.Id, response.UID);
            }
            else
            {
                var errorEmbed = new EmbedBuilder
                {
                    Title = "❌ Profile Creation Failed",
                    Description = response.ErrorMessage ?? "An unknown error occurred",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                _logger.LogWarning("Profile creation failed for user {Username} ({UserId}): {ErrorMessage}", modal.User.Username, modal.User.Id, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling create profile modal for user {UserId}", modal.User.Id);
            await modal.RespondAsync("An unexpected error occurred while creating your profile. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleDeleteProfileModalAsync(SocketModal modal)
    {
        try
        {
            var profileUid = modal.Data.Components.FirstOrDefault(x => x.CustomId == "profile_uid")?.Value?.Trim();
            var confirmation = modal.Data.Components.FirstOrDefault(x => x.CustomId == "confirmation")?.Value?.Trim().ToUpper();

            if (confirmation != "DELETE")
            {
                await modal.RespondAsync("Confirmation text incorrect. Profile deletion cancelled.", ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(profileUid))
            {
                await modal.RespondAsync("Profile UID is required.", ephemeral: true);
                return;
            }

            // UIDs must always be 10 characters and alpha numeric, if otherwise it's an error.
            if (profileUid.Length != 10 || !profileUid.All(char.IsLetterOrDigit))
            {
                await modal.RespondAsync("Invalid UID format. UIDs should be 10 alphanumeric characters.", ephemeral: true);
                return;
            }

            var response = await _registrationService.DeleteUID(modal.User.Id, profileUid);

            if (response.Success)
            {
                var embed = new EmbedBuilder
                {
                    Title = "✅ Profile Deleted Successfully",
                    Description = $"Profile `{profileUid}` has been permanently deleted.",
                    Color = Color.Green
                };

                var buttonBuilder = new ComponentBuilder()
                    .WithButton("🔧 Back to Account Management", "register_start", ButtonStyle.Secondary);

                await modal.RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
                _logger.LogInformation("User {Username} ({UserId}) deleted profile {ProfileUid}", modal.User.Username, modal.User.Id, profileUid);
            }
            else
            {
                var errorEmbed = new EmbedBuilder
                {
                    Title = "❌ Profile Deletion Failed",
                    Description = response.ErrorMessage ?? "An unknown error occurred",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                _logger.LogWarning("Profile deletion failed for user {Username} ({UserId}), UID {ProfileUid}: {ErrorMessage}", modal.User.Username, modal.User.Id, profileUid, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete profile modal for user {UserId}", modal.User.Id);
            await modal.RespondAsync("An unexpected error occurred while deleting your profile. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleRegenerateSecretModalAsync(SocketModal modal)
    {
        try
        {
            var confirmation = modal.Data.Components.FirstOrDefault(x => x.CustomId == "confirmation")?.Value?.Trim().ToUpper();

            if (confirmation != "CONFIRM")
            {
                await modal.RespondAsync("Confirmation text incorrect. Secret regeneration cancelled.", ephemeral: true);
                return;
            }

            var response = await _registrationService.RegenerateSecretAsync(modal.User.Id);

            if (response.Success)
            {
                var embed = new EmbedBuilder
                {
                    Title = "✅ Secret Regenerated Successfully",
                    Description = $"Your new secret has been generated.\n\n**New Secret:** `{response.Secret}`\n\n⚠️ **Important:** All previous sessions are now invalid. You must update your secret in the game client.",
                    Color = Color.Green
                };

                embed.AddField("⚠️ Security Warning",
                    "• All active sessions have been invalidated\n" +
                    "• You must update your secret in the game client\n" +
                    "• Never share your secret with anyone!",
                    inline: false);

                embed.WithFooter("KinkLink Bot • Security Management");

                var buttonBuilder = new ComponentBuilder()
                    .WithButton("🔧 Back to Account Management", "register_start", ButtonStyle.Secondary);

                await modal.RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
                _logger.LogInformation("User {Username} ({UserId}) regenerated their secret", modal.User.Username, modal.User.Id);
            }
            else
            {
                var errorEmbed = new EmbedBuilder
                {
                    Title = "❌ Secret Regeneration Failed",
                    Description = response.ErrorMessage ?? "An unknown error occurred",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                _logger.LogWarning("Secret regeneration failed for user {Username} ({UserId}): {ErrorMessage}", modal.User.Username, modal.User.Id, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling regenerate secret modal for user {UserId}", modal.User.Id);
            await modal.RespondAsync("An unexpected error occurred while regenerating your secret. Please try again later.", ephemeral: true);
        }
    }

    private async Task HandleDeleteAccountModalAsync(SocketModal modal)
    {
        try
        {
            var confirmation = modal.Data.Components.FirstOrDefault(x => x.CustomId == "confirmation")?.Value?.Trim();

            if (confirmation != "YES I WANT TO DELETE")
            {
                await modal.RespondAsync("Confirmation text incorrect. Account deletion cancelled.", ephemeral: true);
                return;
            }

            var response = await _registrationService.RemoveAccount(modal.User.Id);

            if (response.Success)
            {
                var embed = new EmbedBuilder
                {
                    Title = "✅ Account Deleted Successfully",
                    Description = "Your KinkLink account and all associated data have been permanently deleted.",
                    Color = Color.Red
                };

                embed.AddField("What was deleted:",
                    "• Your account\n" +
                    "• All your profiles/UIDs\n" +
                    "• All pair relationships\n" +
                    "• All permission settings",
                    inline: false);

                embed.WithFooter("This action cannot be undone.");

                await modal.RespondAsync(embed: embed.Build(), ephemeral: true);
                _logger.LogInformation("User {Username} ({UserId}) deleted their account", modal.User.Username, modal.User.Id);
            }
            else
            {
                var errorEmbed = new EmbedBuilder
                {
                    Title = "❌ Account Deletion Failed",
                    Description = response.ErrorMessage ?? "An unknown error occurred",
                    Color = Color.Red
                };

                await modal.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                _logger.LogWarning("Account deletion failed for user {Username} ({UserId}): {ErrorMessage}", modal.User.Username, modal.User.Id, response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete account modal for user {UserId}", modal.User.Id);
            await modal.RespondAsync("An unexpected error occurred while deleting your account. Please try again later.", ephemeral: true);
        }
    }
}
