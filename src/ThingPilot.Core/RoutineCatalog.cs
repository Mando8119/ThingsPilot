namespace ThingPilot.Core;

/// <summary>Keeps searches in memory and serializes interaction with the SmartThings UI.</summary>
public sealed class RoutineCatalog(IRoutineBridge bridge, IEnumerable<Routine>? cached = null)
{
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly SemaphoreSlim executionGate = new(1, 1);
    private Routine[] routines = cached?.ToArray() ?? [];
    private DateTimeOffset lastRefresh;
    private DateTimeOffset lastAttempt;

    public event Action? Changed;
    public event Action<Routine[]>? Refreshed;
    public Routine[] Routines => Volatile.Read(ref routines);
    public string Status { get; private set; } = cached?.Any() == true
        ? "Showing the last discovered routines. Refresh to check SmartThings."
        : "Open SmartThings and refresh to discover manually run routines.";
    public bool Loading { get; private set; }

    public async Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await refreshGate.WaitAsync(cancellationToken);
        var notify = false;
        try
        {
            if (!force && DateTimeOffset.UtcNow - lastRefresh < TimeSpan.FromMinutes(30)) return;
            if (!force && DateTimeOffset.UtcNow - lastAttempt < TimeSpan.FromSeconds(15)) return;
            lastAttempt = DateTimeOffset.UtcNow;
            notify = true;
            Loading = true;
            Changed?.Invoke();
            var discovered = await bridge.DiscoverAsync(cancellationToken);
            Volatile.Write(ref routines, discovered);
            lastRefresh = DateTimeOffset.UtcNow;
            Status = discovered.Length == 0
                ? "No manually run routines are visible in the current SmartThings location."
                : $"{discovered.Length} routines · {discovered[0].LocationName} · updated {lastRefresh.ToLocalTime():t}";
            Refreshed?.Invoke(discovered);
        }
        catch (BridgeFailure failure)
        {
            Status = failure.Message;
            if (Routines.Length > 0) Status += " Showing the last discovered list.";
        }
        finally
        {
            Loading = false;
            refreshGate.Release();
            if (notify) Changed?.Invoke();
        }
    }

    public async Task<string> RunAsync(Routine routine, CancellationToken cancellationToken = default)
    {
        if (!await executionGate.WaitAsync(0, cancellationToken))
            return "Another routine is being triggered. Wait for its result.";
        try
        {
            if (!Routines.Any(candidate => candidate.Id == routine.Id))
                return "This routine is no longer in the local list. Refresh routines.";
            await bridge.TriggerAsync(routine, cancellationToken);
            if (bridge.IsDemo) return $"Demo: {routine.Name}. Simulated only; no SmartThings routine or device was triggered.";
            return $"Play invoked: {routine.Name} · {routine.LocationName}. SmartThings does not report when every action finishes.";
        }
        catch (BridgeFailure failure)
        {
            if (failure.Kind == BridgeFailureKind.RoutineUnavailable)
            {
                Volatile.Write(ref routines, Routines.Where(candidate => candidate.Id != routine.Id).ToArray());
                Refreshed?.Invoke(Routines);
                Changed?.Invoke();
            }
            return failure.Message;
        }
        finally { executionGate.Release(); }
    }
}
