using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ThingPilot.Core;

namespace ThingPilot.Extension;

internal sealed partial class RoutinePage : DynamicListPage, IDisposable
{
    private readonly RoutineCatalog catalog;
    private readonly bool demo;
    private readonly RoutinePage? demoPage;

    public RoutinePage(RoutineCatalog catalog, bool demo = false)
    {
        this.catalog = catalog;
        this.demo = demo;
        if (!demo) demoPage = new RoutinePage(new RoutineCatalog(new DemoRoutineBridge(), DemoRoutineBridge.Samples), true);
        Id = demo ? "ThingPilot.Demo" : "ThingPilot.Routines";
        Title = demo ? "ThingPilot demo — no devices controlled" : "ThingPilot";
        Name = "Open routines";
        Icon = new IconInfo("\uE945");
        PlaceholderText = "Search routines or locations";
        catalog.Changed += Changed;
        Changed();
    }

    private void Changed()
    {
        IsLoading = catalog.Loading;
        EmptyContent = new CommandItem(new RefreshCommand(catalog))
        {
            Title = catalog.Loading ? "Reading SmartThings…" : "Refresh manually run routines",
            Subtitle = catalog.Status
        };
        RaiseItemsChanged();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

    public override IListItem[] GetItems()
    {
        var matches = RoutineSearch.Find(catalog.Routines, SearchText);
        return [
            ..(matches.Length == 0 ? new IListItem[] { new ListItem(new NoOpCommand()) {
                Title = catalog.Loading ? "Reading SmartThings…" : catalog.Routines.Length == 0
                    ? "No manually run routines discovered" : "No matching routines",
                Subtitle = string.IsNullOrWhiteSpace(SearchText) ? catalog.Status : "Try another routine name or location.",
                Section = "Routines"
            } } : []),
            ..matches.Select(routine => new ListItem(new RunRoutineCommand(catalog, routine)) {
                Title = routine.Name, Subtitle = routine.Context, Section = "Routines" }),
            new ListItem(new RefreshCommand(catalog)) {
                Title = "Refresh from SmartThings",
                Subtitle = demo ? "Demo only: reload sample routines" : catalog.Status,
                Section = "ThingPilot"
            },
            ..(!demo ? new IListItem[] { new ListItem(new OpenSmartThingsCommand()) {
                Title = "Open SmartThings",
                Subtitle = "Sign in or switch location before refreshing ThingPilot",
                Section = "ThingPilot"
            }, new ListItem(demoPage!) { Title = "Try demo routines", Subtitle = "Preview search and simulated execution without a Samsung account", Section = "ThingPilot" } } : [])
        ];
    }

    public void Dispose() { catalog.Changed -= Changed; demoPage?.Dispose(); }
}

internal sealed partial class RefreshCommand : InvokableCommand
{
    private readonly RoutineCatalog catalog;
    private int busy;

    public RefreshCommand(RoutineCatalog catalog)
    {
        this.catalog = catalog;
        Name = "Refresh routines";
        Id = "ThingPilot.Refresh";
    }

    public override ICommandResult Invoke()
    {
        if (Interlocked.Exchange(ref busy, 1) != 0) return CommandResult.KeepOpen();
        Name = "Refreshing routines";
        _ = RefreshAsync();
        return CommandResult.KeepOpen();
    }

    private async Task RefreshAsync()
    {
        try { await catalog.RefreshAsync(true); UiFeedback.Show(catalog.Status); }
        catch (Exception) { UiFeedback.Show("ThingPilot could not read SmartThings. Open the app and try again."); }
        finally { Name = "Refresh routines"; Volatile.Write(ref busy, 0); }
    }
}

internal sealed partial class OpenSmartThingsCommand : InvokableCommand
{
    public OpenSmartThingsCommand() { Name = "Open SmartThings"; }

    public override ICommandResult Invoke()
    {
        try { SmartThingsWindowsBridge.LaunchApp();
            return CommandResult.Dismiss(); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException or System.Runtime.InteropServices.COMException)
        { return CommandResult.ShowToast("SmartThings could not open. Install it from Microsoft Store, then sign in."); }
    }
}
