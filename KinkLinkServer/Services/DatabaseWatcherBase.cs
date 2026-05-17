using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KinkLinkServer.Services;

public abstract class DatabaseWatcherBase : IHostedService, IDisposable, IAsyncDisposable
{
    private static readonly Regex ValidChannelNameRegex = new(
        @"^[a-zA-Z_][a-zA-Z0-9_]{0,63}$", RegexOptions.Compiled
    );

    private readonly string _connectionString;
    private readonly ILogger _logger;
    private NpgsqlConnection? _connection;
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private Task? _consumerTask;
    private readonly Channel<NpgsqlNotificationEventArgs> _notificationChannel =
        Channel.CreateBounded<NpgsqlNotificationEventArgs>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    private bool _channelNameValidated;

    protected readonly IHubContext<PrimaryHub> HubContext;
    protected readonly IPresenceService PresenceService;
    protected readonly KinkLinkProfilesService ProfilesService;

    protected abstract string ChannelName { get; }

    protected DatabaseWatcherBase(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        ILogger logger)
    {
        _connectionString = config.DatabaseConnectionString;
        HubContext = hubContext;
        PresenceService = presenceService;
        ProfilesService = profilesService;
        _logger = logger;
    }

    protected virtual async Task<string?> GetUidByProfileIdAsync(int profileId)
    {
        return await ProfilesService.GetUidByProfileIdAsync(profileId);
    }

    protected static T? DeserializePayload<T>(string payload) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload);
        }
        catch
        {
            return null;
        }
    }

    protected abstract Task HandleNotificationAsync(string? channel, string payload);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = RunAsync(_cts.Token);
        _consumerTask = ConsumeNotificationsAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_channelNameValidated)
                {
                    if (!ValidChannelNameRegex.IsMatch(ChannelName))
                        throw new InvalidOperationException(
                            $"PostgreSQL channel name '{ChannelName}' is invalid");
                    _channelNameValidated = true;
                }

                _connection = new NpgsqlConnection(_connectionString);
                _connection.Notification += OnNotification;
                await _connection.OpenAsync(ct);

                await using var listenCmd = new NpgsqlCommand($"LISTEN \"{ChannelName}\"", _connection);
                await listenCmd.ExecuteNonQueryAsync(ct);

                _logger.LogInformation("[Watcher] LISTEN on {Channel}", ChannelName);

                while (!ct.IsCancellationRequested)
                {
                    await _connection.WaitAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "[Watcher] Connection lost on {Channel}, reconnecting in 5s", ChannelName);

                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                try
                {
                    var delay = 5000 + Random.Shared.Next(0, 3000);
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void OnNotification(object? sender, NpgsqlNotificationEventArgs e)
    {
        // Log receipt of the notification and attempt to enqueue it for processing
        try
        {
            _logger.LogDebug("[Watcher] Notification received on {Channel}: {Payload}", e.Channel, e.Payload);
        }
        catch
        {
            // Swallow any logging formatting errors to avoid breaking notification handling
        }

        var written = _notificationChannel.Writer.TryWrite(e);
        if (!written)
        {
            _logger.LogWarning("[Watcher] Notification channel full or write failed for {Channel}; dropping notification", e.Channel);
        }
    }

    private async Task ConsumeNotificationsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await foreach (var e in _notificationChannel.Reader.ReadAllAsync(ct))
                {
                    try
                    {
                        _logger.LogDebug("[Watcher] Consuming notification on {Channel} (payload length {Len})", e.Channel, e.Payload?.Length ?? 0);
                        await HandleNotificationAsync(e.Channel, e.Payload ?? "");
                        // Handled: this is an important event worth logging at Information level so it shows up even when Debug is disabled
                        _logger.LogInformation("[Watcher] Handled notification for {Channel}", e.Channel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Watcher] Failed to handle notification on {Channel}", e.Channel);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "[Watcher] Consumer loop faulted on {Channel}, restarting", ChannelName);
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop listener
        _cts?.Cancel();

        if (_runTask != null)
        {
            try
            {
                await _runTask;
            }
            catch (OperationCanceledException) { }
        }

        // No more notifications will arrive — signal consumer to drain and exit
        if (!_notificationChannel.Writer.TryComplete())
        {
            _logger.LogWarning("[Watcher] Notification channel already completed for {Channel}", ChannelName);
        }

        if (_consumerTask != null)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
                await _consumerTask.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) { }
        }

        (_notificationChannel as IDisposable)?.Dispose();

        if (_connection != null)
        {
            try
            {
                await using var unlistenCmd = new NpgsqlCommand($"UNLISTEN \"{ChannelName}\"", _connection);
                await unlistenCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch { }
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _connection?.Dispose();
        (_notificationChannel as IDisposable)?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        Dispose();
        GC.SuppressFinalize(this);
    }
}
