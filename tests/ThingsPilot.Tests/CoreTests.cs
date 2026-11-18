using ThingsPilot.Core;

namespace ThingsPilot.Tests;

public class CoreTests
{
    [Fact]
    public void SearchRanksExactNamesAndDistinguishesLocations()
    {
        var routines = new[]
        {
            Routine.Create("Good Night Lights", "Home"),
            Routine.Create("Good Night", "Cabin"),
            Routine.Create("Good Night", "Home")
        };
        var matches = RoutineSearch.Find(routines, "good night");
        Assert.Equal(new[] { "Cabin", "Home", "Home" }, matches.Select(routine => routine.LocationName));
        Assert.Equal("Cabin", Assert.Single(RoutineSearch.Find(routines, "NIGHT cabin")).LocationName);
        Assert.Empty(RoutineSearch.Find(routines, "missing"));
        Assert.Equal(3, RoutineSearch.Find(routines, "  ").Length);
    }

    [Fact]
    public void StableIdentityUsesNameAndLocation()
    {
        Assert.Equal(Routine.Create("Good Night", "Home").Id, Routine.Create("Good Night", "Home").Id);
        Assert.NotEqual(Routine.Create("Good Night", "Home").Id, Routine.Create("Good Night", "Cabin").Id);
    }

    [Fact]
    public async Task RefreshReplacesCacheAndPublishesIt()
    {
        var expected = Routine.Create("Good Night", "My home");
        var bridge = new FakeBridge([expected]);
        var catalog = new RoutineCatalog(bridge, [Routine.Create("Old", "My home")]);
        Routine[]? published = null;
        catalog.Refreshed += routines => published = routines;

        await catalog.RefreshAsync(true);

        Assert.Equal([expected], catalog.Routines);
        Assert.Equal([expected], Assert.IsType<Routine[]>(published));
        Assert.Contains("My home", catalog.Status);
        Assert.Equal(1, bridge.Discoveries);
    }

    [Fact]
    public async Task TriggerIsSentOnceAndReportsOnlyPlayInvocation()
    {
        var routine = Routine.Create("Good Night", "My home");
        var bridge = new FakeBridge([routine]);
        var catalog = new RoutineCatalog(bridge, [routine]);

        var result = await catalog.RunAsync(routine);

        Assert.Equal(1, bridge.Triggers);
        Assert.Contains("Play invoked", result);
        Assert.Contains("does not report", result);
    }

    [Fact]
    public async Task MissingRoutineFailureRemovesStaleCommand()
    {
        var routine = Routine.Create("Good Night", "My home");
        var bridge = new FakeBridge([routine]) { TriggerFailure = new BridgeFailure("gone", BridgeFailureKind.RoutineUnavailable) };
        var catalog = new RoutineCatalog(bridge, [routine]);

        Assert.Equal("gone", await catalog.RunAsync(routine));
        Assert.Empty(catalog.Routines);
    }

    [Fact]
    public async Task FailedDiscoveryRetainsCacheAndShowsFailure()
    {
        var routine = Routine.Create("Study", "Home");
        var catalog = new RoutineCatalog(new FailedBridge(), [routine]);
        await catalog.RefreshAsync(true);
        Assert.Equal(routine, Assert.Single(catalog.Routines));
        Assert.Contains("Sign in", catalog.Status);
        Assert.Contains("last discovered list", catalog.Status);
        Assert.False(catalog.Loading);
    }

    [Fact]
    public async Task DemoExecutionIsExplicitlySimulated()
    {
        var catalog = new RoutineCatalog(new DemoRoutineBridge());
        await catalog.RefreshAsync(true);
        var result = await catalog.RunAsync(catalog.Routines[0]);
        Assert.Contains("Simulated only", result);
        Assert.DoesNotContain("Play invoked", result);
    }

    [Fact]
    public async Task MissingRoutineIsRemovedFromPersistentCache()
    {
        var routine = Routine.Create("Study", "Home");
        var bridge = new FakeBridge([routine]) { TriggerFailure = new BridgeFailure("removed", BridgeFailureKind.RoutineUnavailable) };
        var catalog = new RoutineCatalog(bridge, [routine]);
        Routine[]? persisted = null;
        catalog.Refreshed += list => persisted = list;
        await catalog.RunAsync(routine);
        Assert.Empty(Assert.IsType<Routine[]>(persisted));
    }

    private sealed class FailedBridge : IRoutineBridge
    {
        public Task<Routine[]> DiscoverAsync(CancellationToken cancellationToken = default) =>
            throw new BridgeFailure("Sign in to SmartThings.");
        public Task TriggerAsync(Routine routine, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task DuplicateExecutionIsSuppressed()
    {
        var routine = Routine.Create("Good Night", "My home");
        var bridge = new FakeBridge([routine]) { BlockTrigger = true };
        var catalog = new RoutineCatalog(bridge, [routine]);
        var first = catalog.RunAsync(routine);
        await bridge.TriggerStarted.Task;

        Assert.Contains("Another routine", await catalog.RunAsync(routine));
        bridge.ReleaseTrigger.SetResult();
        await first;
        Assert.Equal(1, bridge.Triggers);
    }

    private sealed class FakeBridge(Routine[] routines) : IRoutineBridge
    {
        public int Discoveries { get; private set; }
        public int Triggers { get; private set; }
        public BridgeFailure? TriggerFailure { get; init; }
        public bool BlockTrigger { get; init; }
        public TaskCompletionSource TriggerStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseTrigger { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Routine[]> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            Discoveries++;
            return Task.FromResult(routines);
        }

        public async Task TriggerAsync(Routine routine, CancellationToken cancellationToken = default)
        {
            Triggers++;
            TriggerStarted.TrySetResult();
            if (BlockTrigger) await ReleaseTrigger.Task.WaitAsync(cancellationToken);
            if (TriggerFailure is not null) throw TriggerFailure;
        }
    }
}
