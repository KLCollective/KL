using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Glamourer.Api.IpcSubscribers;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using Timer = System.Timers.Timer;

namespace KinkLinkClient.Handlers;

public class GlamourerEventHandler : IDisposable
{
    private readonly GlamourerService _glamourerService;
    private readonly WardrobeManager _wardrobeManager;

    /// <summary>
    ///     Channel that coalesces rapid state-changed signals. Capacity 1 with DropOldest
    ///     means only the latest pending signal is kept — intermediate events are dropped.
    /// </summary>
    private readonly Channel<byte> _eventSignal = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest }
    );

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _consumerTask;
    private readonly Timer _debounceTimer;

    /// <summary>
    ///     Set to true when a finalized event arrives, instructing the consumer to
    ///     bypass the 50ms debounce and process the equipment immediately.
    /// </summary>
    private volatile bool _finalizedPending;

    /// <summary>
    ///     Re-entrancy guard. Set while <see cref="ProcessEquipmentAsync"/> is executing
    ///     to suppress self-triggered Glamourer state-changed events from our own apply calls.
    /// </summary>
    private volatile bool _processing;

    private const int DebounceMs = 500;

    public GlamourerEventHandler(GlamourerService glamourerService, WardrobeManager wardrobeManager)
    {
        _glamourerService = glamourerService;
        _wardrobeManager = wardrobeManager;

        _debounceTimer = new Timer(DebounceMs) { AutoReset = false };
        _debounceTimer.Elapsed += OnDebounceElapsed;

        _glamourerService.OnStateChangedWithType.Event += OnStateChangedWithType;
        _glamourerService.OnStateFinalizedWithType.Event += OnStateFinalizedWithType;

        _consumerTask = ConsumeEventsAsync(_cts.Token);
    }

    private unsafe bool IsLocalPlayer(nint address)
    {
        return address == (nint)Control.Instance()->LocalPlayer;
    }

    // ── Producers ──────────────────────────────────────────────

    /// <summary>
    ///     Fires on every Glamourer state change (gear swap, dye change, etc.).
    ///     Resets the debounce timer so the consumer waits ~50ms after the last change.
    /// </summary>
    public void OnStateChangedWithType(nint address, Glamourer.Api.Enums.StateChangeType state)
    {
        if (!IsLocalPlayer(address) || _processing)
            return;

        // Reset debounce: each state change pushes the processing window forward
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    /// <summary>
    ///     Fires when Glamourer finalizes a state change (gear fully resolved).
    ///     Immediately signals the consumer to bypass any pending debounce.
    /// </summary>
    public void OnStateFinalizedWithType(
        nint address,
        Glamourer.Api.Enums.StateFinalizationType state
    )
    {
        if (!IsLocalPlayer(address) || _processing)
            return;

        _finalizedPending = true;
        _debounceTimer.Stop();
        _eventSignal.Writer.TryWrite(0);
    }

    // ── Consumer ───────────────────────────────────────────────

    /// <summary>
    ///     Single consumer loop. Waits for signals — either from the debounce timer
    ///     (state changed batch completed) or from a finalized event (process now).
    ///     Serialises all <see cref="ProcessEquipmentAsync"/> calls.
    /// </summary>
    private async Task ConsumeEventsAsync(CancellationToken ct)
    {
        while (await _eventSignal.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            // Drain any accumulated signals — we only care about the latest state
            while (_eventSignal.Reader.TryRead(out _)) { }

            try
            {
                await ProcessEquipmentAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "[GlamourerEventHandler] Error in consumer loop");
            }
        }
    }

    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        _eventSignal.Writer.TryWrite(0);
    }

    // ── Processing ─────────────────────────────────────────────

    private async Task ProcessEquipmentAsync()
    {
        _processing = true;
        try
        {
            // Consume the finalized flag — subsequent calls will re-debounce
            Interlocked.Exchange(ref _finalizedPending, false);

            var jobject = await _glamourerService
                .GetDesignComponentsAsync(GlamourerService.PLAYER_ID)
                .ConfigureAwait(false);

            var design = GlamourerDesignHelper.FromJObject(jobject);
            if (design != null)
                await _wardrobeManager.ReapplyIfChanged(design).ConfigureAwait(false);
        }
        finally
        {
            _processing = false;
        }
    }

    public void Dispose()
    {
        _glamourerService.OnStateChangedWithType.Event -= OnStateChangedWithType;
        _glamourerService.OnStateFinalizedWithType.Event -= OnStateFinalizedWithType;

        _debounceTimer.Stop();
        _debounceTimer.Dispose();

        _cts.Cancel();
        _cts.Dispose();

        // Fire-and-forget the consumer task completion — we don't block dispose on it
        _consumerTask.ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

        GC.SuppressFinalize(this);
    }
}
