using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ThingPilot.Core;

namespace ThingPilot.Extension;

public sealed partial class ThingPilotProvider : CommandProvider
{
    private readonly RoutineCache cache = new();
    private readonly RoutineCatalog catalog;
    private readonly RoutinePage page;
    private int disposed;

    public ThingPilotProvider()
    {
        Id = "ThingPilot";
        DisplayName = "ThingPilot";
        Icon = new IconInfo("\uE945");
        Frozen = false;
        var cached = cache.Load();
        catalog = new RoutineCatalog(new SmartThingsWindowsBridge(), cached);
        page = new RoutinePage(catalog);
        catalog.Changed += OnChanged;
        catalog.Refreshed += cache.Save;
        // Discovery is explicit: merely loading the extension must not open another app.
    }

    private void OnChanged() => RaiseItemsChanged();

    public override ICommandItem[] TopLevelCommands() => [
        new CommandItem(page) { Title = "ThingPilot", Subtitle = catalog.Status },
        ..catalog.Routines.Select(routine => new CommandItem(new RunRoutineCommand(catalog, routine))
        { Title = routine.Name, Subtitle = routine.Context })
    ];

    public override ICommand? GetCommand(string id) => id == page.Id ? page : catalog.Routines
        .Where(routine => "ThingPilot.Routine." + routine.Id == id)
        .Select(routine => (ICommand)new RunRoutineCommand(catalog, routine)).FirstOrDefault();

    public override void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        page.Dispose();
        catalog.Changed -= OnChanged;
        catalog.Refreshed -= cache.Save;
    }
}

internal sealed partial class RunRoutineCommand : InvokableCommand
{
    private readonly RoutineCatalog catalog;
    private readonly Routine routine;
    private int busy;

    public RunRoutineCommand(RoutineCatalog catalog, Routine routine)
    {
        this.catalog = catalog;
        this.routine = routine;
        Id = "ThingPilot.Routine." + routine.Id;
        Name = "Run routine";
        Icon = new IconInfo("\uE768");
    }

    public override ICommandResult Invoke()
    {
        if (Interlocked.Exchange(ref busy, 1) != 0) return CommandResult.KeepOpen();
        Name = "Invoking Play…";
        _ = RunAsync();
        return CommandResult.KeepOpen();
    }

    private async Task RunAsync()
    {
        try { UiFeedback.Show(await catalog.RunAsync(routine)); }
        catch (Exception) { UiFeedback.Show("ThingPilot could not invoke Play. Open SmartThings and check the routine before retrying."); }
        finally { Name = "Run routine"; Volatile.Write(ref busy, 0); }
    }
}

internal static class UiFeedback
{
    public static void Show(string message) => new ToastStatusMessage(message) { Duration = 7000 }.Show();
}
