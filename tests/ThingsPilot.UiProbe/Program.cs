namespace ThingsPilot.Extension;

internal static class Program
{
    [MTAThread]
    private static async Task<int> Main(string[] args)
    {
        if (args.Length != 0 && (args.Length != 2 || args[0] != "--run"))
            throw new ArgumentException("Use no arguments for read-only checks, or --run \"Routine name\" for an explicit live test.");
        var bridge = new SmartThingsWindowsBridge();
        var routines = await bridge.DiscoverAsync();
        if (routines.Length == 0) throw new Exception("No manually run routines were exposed by SmartThings.");
        if (routines.Any(routine => string.IsNullOrWhiteSpace(routine.Name) || string.IsNullOrWhiteSpace(routine.LocationName)))
            throw new Exception("SmartThings exposed an incomplete routine entry.");
        foreach (var routine in routines) await bridge.InspectAsync(routine);
        if (args.Length == 2)
        {
            var target = routines.Single(routine => routine.Name.Equals(args[1], StringComparison.Ordinal));
            await bridge.TriggerAsync(target);
            Console.WriteLine($"LIVE TEST: sent one Play activation for {target.Name}. Verify effects independently; completion is not confirmed by ThingsPilot.");
            return 0;
        }
        Console.WriteLine($"PASS: SmartThings exposed {routines.Length} manually run routines in {routines[0].LocationName}; verified each Play lookup and keyboard eligibility without activating it. No routine was triggered.");
        return 0;
    }
}
