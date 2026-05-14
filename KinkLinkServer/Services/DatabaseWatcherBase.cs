using System.Data;
using System.Text.Json;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KinkLinkServer.Services;

public abstract class DatabaseWatcherBase : IHostedService, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger _logger;
    private NpgsqlConnection? _connection;
    private CancellationTokenSource? _cts;
    private Task? _runTask;

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
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
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
                    await Task.Delay(5000, ct);
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
        _ = HandleNotificationAsync(e.Channel, e.Payload ?? "");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();

        if (_runTask != null)
        {
            try
            {
                await _runTask;
            }
            catch (OperationCanceledException) { }
        }

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
        _cts?.Cancel();
        _cts?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
