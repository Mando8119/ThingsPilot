using System.Security.Cryptography;
using System.Text;

namespace ThingsPilot.Core;

public sealed record Routine(string Id, string Name, string LocationName)
{
    public string Context => $"{LocationName} · Manually run routine";

    public static Routine Create(string name, string locationName)
    {
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{locationName}\0{name}")))[..24];
        return new Routine(identity, name, locationName);
    }
}

public interface IRoutineBridge
{
    bool IsDemo => false;
    Task<Routine[]> DiscoverAsync(CancellationToken cancellationToken = default);
    Task TriggerAsync(Routine routine, CancellationToken cancellationToken = default);
}

public sealed class DemoRoutineBridge : IRoutineBridge
{
    public bool IsDemo => true;
    public static Routine[] Samples => [Routine.Create("Good Night", "Demo Home"),
        Routine.Create("Study", "Demo Home"), Routine.Create("Good Night", "Demo Cabin")];
    public Task<Routine[]> DiscoverAsync(CancellationToken cancellationToken = default) => Task.FromResult(Samples);
    public Task TriggerAsync(Routine routine, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public enum BridgeFailureKind { NotInstalled, NotSignedIn, AppChanged, RoutineUnavailable, WrongLocation, OperationFailed }

public sealed class BridgeFailure(string message, BridgeFailureKind kind = BridgeFailureKind.OperationFailed, Exception? inner = null) : Exception(message, inner)
{
    public BridgeFailureKind Kind { get; } = kind;
}

public static class RoutineSearch
{
    public static Routine[] Find(IEnumerable<Routine> routines, string query)
    {
        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return routines.Select(r => (Routine: r, Score: Score(r, terms, query.Trim())))
            .Where(x => x.Score >= 0).OrderByDescending(x => x.Score)
            .ThenBy(x => x.Routine.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Routine.LocationName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Routine.Id, StringComparer.Ordinal).Select(x => x.Routine).ToArray();
    }

    private static int Score(Routine routine, string[] terms, string query)
    {
        if (!terms.All(t => routine.Name.Contains(t, StringComparison.CurrentCultureIgnoreCase) ||
                            routine.LocationName.Contains(t, StringComparison.CurrentCultureIgnoreCase))) return -1;
        if (routine.Name.Equals(query, StringComparison.CurrentCultureIgnoreCase)) return 100;
        if (routine.Name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)) return 80;
        if (routine.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)) return 60;
        return 20;
    }
}
