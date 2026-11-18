using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using ThingsPilot.Core;

namespace ThingsPilot.Extension;

/// <summary>Uses the documented Windows UI Automation client API against the installed SmartThings app.</summary>
internal sealed partial class SmartThingsWindowsBridge : IRoutineBridge
{
    private const string AppId = "SAMSUNGELECTRONICSCO.LTD.SmartThingsWindows_3c1yjt4zspk6g!App";
    private readonly SemaphoreSlim appGate = new(1, 1);

    public async Task<Routine[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        await appGate.WaitAsync(cancellationToken);
        try { return await Task.Run(() => Discover(cancellationToken), cancellationToken); }
        catch (Exception error) when (IsAutomationFailure(error))
        { throw new BridgeFailure("SmartThings could not be read. Close any dialog, check your sign-in, then refresh.", inner: error); }
        finally { appGate.Release(); }
    }

    public async Task TriggerAsync(Routine routine, CancellationToken cancellationToken = default)
    {
        await appGate.WaitAsync(cancellationToken);
        try { await Task.Run(() => Trigger(routine, cancellationToken), cancellationToken); }
        catch (Exception error) when (IsAutomationFailure(error))
        { throw new BridgeFailure("Play could not be confirmed. The routine may have started; check SmartThings before retrying.", inner: error); }
        finally { appGate.Release(); }
    }

    // Used by the local acceptance probe. Performs the same lookup and checks without invoking Play.
    internal async Task InspectAsync(Routine routine, CancellationToken cancellationToken = default)
    {
        await appGate.WaitAsync(cancellationToken);
        try { await Task.Run(() => Trigger(routine, cancellationToken, false), cancellationToken); }
        finally { appGate.Release(); }
    }

    private static Routine[] Discover(CancellationToken cancellationToken)
    {
        using var session = OpenAndFindWindow(cancellationToken);
        var window = session.Window;
        OpenRoutinesPage(window, cancellationToken);
        var location = FindById(window, "LocationNameTextBlock")?.Current.Name?.Trim();
        if (string.IsNullOrWhiteSpace(location))
            throw new BridgeFailure("Sign in to the SmartThings Windows app, choose a location, then refresh.", BridgeFailureKind.NotSignedIn);

        var scenes = FindById(window, "ScenesGridView");
        if (scenes is null)
            throw new BridgeFailure("ThingsPilot could not find SmartThings' Manually run routines list. Update or reopen SmartThings and try again.", BridgeFailureKind.AppChanged);

        var listItems = ReadItems(scenes, cancellationToken);
        var names = listItems
            .Select(item => item.Current.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new BridgeFailure("Two manual routines in this location have the same name. Give them unique names in SmartThings before refreshing.", BridgeFailureKind.AppChanged);
        return names
            .Select(name => Routine.Create(name!, location))
            .OrderBy(routine => routine.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static void Trigger(Routine routine, CancellationToken cancellationToken, bool invokePlay = true)
    {
        using var session = OpenAndFindWindow(cancellationToken);
        var window = session.Window;
        OpenRoutinesPage(window, cancellationToken);
        var currentLocation = FindById(window, "LocationNameTextBlock")?.Current.Name?.Trim();
        if (!string.Equals(currentLocation, routine.LocationName, StringComparison.CurrentCultureIgnoreCase))
            throw new BridgeFailure($"Switch SmartThings to {routine.LocationName}, refresh ThingsPilot, then run {routine.Name}.", BridgeFailureKind.WrongLocation);

        var scenes = FindById(window, "ScenesGridView");
        var matches = scenes is null ? [] : ReadItems(scenes, cancellationToken)
            .Where(candidate => string.Equals(candidate.Current.Name?.Trim(), routine.Name, StringComparison.Ordinal)).ToArray();
        if (matches.Length > 1)
            throw new BridgeFailure("More than one manual routine has this name. Rename them in SmartThings before trying again.");
        var item = matches.SingleOrDefault();
        if (item is not null && item.TryGetCurrentPattern(VirtualizedItemPattern.Pattern, out var virtualPattern))
            ((VirtualizedItemPattern)virtualPattern).Realize();
        var play = item is null ? null : FindById(item, "ActionButton");
        if (play is null)
            throw new BridgeFailure($"{routine.Name} is no longer available in {routine.LocationName}. Refresh ThingsPilot.", BridgeFailureKind.RoutineUnavailable);
        if (!play.Current.IsEnabled)
            throw new BridgeFailure("SmartThings has disabled this routine's Play button. Check the app and try again later.");
        if (play.Current.ControlType != ControlType.Button || !play.Current.IsKeyboardFocusable)
            throw new BridgeFailure("SmartThings changed how its Play control works. Update ThingsPilot before trying again.", BridgeFailureKind.AppChanged);

        // Activate exactly once. Input dispatch does not confirm completion of routine actions.
        cancellationToken.ThrowIfCancellationRequested();
        // Recheck location immediately before invoking; never retry this operation.
        if (!string.Equals(FindById(window, "LocationNameTextBlock")?.Current.Name?.Trim(), routine.LocationName, StringComparison.Ordinal))
            throw new BridgeFailure("The SmartThings location changed. Refresh ThingsPilot before running this routine.", BridgeFailureKind.WrongLocation);
        if (invokePlay) PlayActivation.Activate(window, play, cancellationToken);
    }

    private static SmartThingsSession OpenAndFindWindow(CancellationToken cancellationToken)
    {
        var window = FindWindow();
        var launched = window is null;
        var minimizeAfter = launched;
        if (window is not null && window.TryGetCurrentPattern(WindowPattern.Pattern, out var current)
            && current is WindowPattern state && state.Current.WindowVisualState == WindowVisualState.Minimized)
            minimizeAfter = true;
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (launched || attempt > 0) LaunchApp();
                window = WaitFor(() => {
                    var candidate = FindWindow();
                    if (candidate is null) return null;
                    if (candidate.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern) && pattern is WindowPattern restore
                        && restore.Current.WindowVisualState == WindowVisualState.Minimized)
                        restore.SetWindowVisualState(WindowVisualState.Normal);
                    return FindById(candidate, "RoutinesNavItem") is not null ? candidate : null;
                }, TimeSpan.FromSeconds(attempt == 0 ? 5 : 25), cancellationToken);
                if (window is not null) return new SmartThingsSession(window, minimizeAfter);
                // Retry app preparation only: a window may have been closing during activation.
            }
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new BridgeFailure("Install Samsung SmartThings from Microsoft Store, sign in, then refresh ThingsPilot.", BridgeFailureKind.NotInstalled);
        }

        throw new BridgeFailure("SmartThings did not become ready. Finish signing in, close any dialog, then refresh ThingsPilot.", BridgeFailureKind.NotSignedIn);
    }

    internal static void LaunchApp()
    {
        var activation = (IApplicationActivationManager)new ApplicationActivationManager();
        try { Marshal.ThrowExceptionForHR(activation.ActivateApplication(AppId, null, 2, out _)); }
        finally { Marshal.FinalReleaseComObject(activation); }
    }

    [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    private class ApplicationActivationManager { }

    [ComImport, Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string applicationId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments, uint options, out uint processId);
    }

    private static void OpenRoutinesPage(AutomationElement window, CancellationToken cancellationToken)
    {
        var navigationItem = WaitFor(() => FindById(window, "RoutinesNavItem"), TimeSpan.FromSeconds(5), cancellationToken)
            ?? throw new BridgeFailure("ThingsPilot could not find the Routines navigation item. Finish SmartThings setup and try again.", BridgeFailureKind.NotSignedIn);
        if (!navigationItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern) || pattern is not SelectionItemPattern selection)
            throw new BridgeFailure("SmartThings changed its Routines navigation. Update ThingsPilot before trying again.", BridgeFailureKind.AppChanged);
        selection.Select();
        // A selected category can hide routines. The app's first category is its built-in All category.
        var categories = FindById(window, "CategoryListView");
        var allCategory = categories?.FindFirst(TreeScope.Children,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
        if (allCategory is not null && allCategory.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var categoryPattern)
            && categoryPattern is SelectionItemPattern category && !category.Current.IsSelected)
            category.Select();
        if (WaitFor(() => FindById(window, "ScenesGridView"), TimeSpan.FromSeconds(2), cancellationToken) is null)
        {
            var expand = FindById(window, "SceneExpandCollapseButton");
            if (expand is not null && expand.TryGetCurrentPattern(InvokePattern.Pattern, out var expandPattern))
                ((InvokePattern)expandPattern).Invoke();
        }
        if (WaitFor(() => FindById(window, "ScenesGridView"), TimeSpan.FromSeconds(8), cancellationToken) is null)
            throw new BridgeFailure("SmartThings did not show Manually run routines. Check its current location and try again.", BridgeFailureKind.NotSignedIn);
    }

    private static AutomationElement? FindWindow() => AutomationElement.RootElement.FindAll(
            TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "SmartThings"))
        .Cast<AutomationElement>()
        .FirstOrDefault(element => element.Current.ControlType == ControlType.Window && IsSmartThingsWindow(element));

    private static bool IsSmartThingsWindow(AutomationElement element)
    {
        if (IsSmartThingsProcess(element.Current.ProcessId)) return true;
        // UIA omits CoreWindow descendants while minimized; native child HWNDs still identify the UWP app.
        var matched = false;
        EnumChildWindows((nint)element.Current.NativeWindowHandle, (child, _) => {
            GetWindowThreadProcessId(child, out var processId);
            if (!IsSmartThingsProcess((int)processId)) return true;
            matched = true;
            return false;
        }, 0);
        return matched;
    }

    private static bool IsSmartThingsProcess(int processId)
    {
        var handle = OpenProcess(0x1000, false, processId);
        if (handle == 0) return false;
        try
        {
            uint length = 512;
            var id = new StringBuilder((int)length);
            return GetApplicationUserModelId(handle, ref length, id) == 0 && id.ToString() == AppId;
        }
        finally { CloseHandle(handle); }
    }

    private static AutomationElement[] ReadItems(AutomationElement list, CancellationToken cancellationToken)
    {
        var items = new List<AutomationElement>();
        if (list.TryGetCurrentPattern(ItemContainerPattern.Pattern, out var pattern) && pattern is ItemContainerPattern container)
        {
            AutomationElement? previous = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var next = container.FindItemByProperty(previous, null, null);
                if (next is null) break;
                if (items.Count >= 2000 || (previous is not null && Automation.Compare(previous, next)))
                    throw new BridgeFailure("SmartThings returned an incomplete routine list. Reopen the app and refresh.");
                if (next.TryGetCurrentPattern(VirtualizedItemPattern.Pattern, out var virtualized))
                    ((VirtualizedItemPattern)virtualized).Realize();
                items.Add(next);
                previous = next;
            }
            return items.ToArray();
        }
        return list.FindAll(TreeScope.Children,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem)).Cast<AutomationElement>().ToArray();
    }

    private static bool IsAutomationFailure(Exception error) => error is ElementNotAvailableException
        or ElementNotEnabledException or InvalidOperationException or COMException or UnauthorizedAccessException;

    [DllImport("kernel32.dll")]
    private static extern nint OpenProcess(uint access, bool inherit, int processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(nint process, ref uint length, StringBuilder id);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);
    private delegate bool EnumChildCallback(nint window, nint parameter);
    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(nint window, EnumChildCallback callback, nint parameter);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    private static AutomationElement? FindById(AutomationElement root, string automationId) => root.FindFirst(
        TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

    private static T? WaitFor<T>(Func<T?> action, TimeSpan timeout, CancellationToken cancellationToken) where T : class
    {
        var end = DateTime.UtcNow + timeout;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = action();
                if (result is not null) return result;
            }
            catch (ElementNotAvailableException) { }
            if (cancellationToken.WaitHandle.WaitOne(250)) cancellationToken.ThrowIfCancellationRequested();
        } while (DateTime.UtcNow < end);
        return null;
    }

    private sealed partial class SmartThingsSession : IDisposable
    {
        private readonly WindowPattern? windowPattern;
        private readonly WindowVisualState originalState;
        private readonly bool launched;

        public SmartThingsSession(AutomationElement window, bool launched)
        {
            Window = window;
            this.launched = launched;
            if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern) && pattern is WindowPattern available)
            {
                windowPattern = available;
                originalState = available.Current.WindowVisualState;
                if (originalState == WindowVisualState.Minimized)
                    available.SetWindowVisualState(WindowVisualState.Normal);
            }
        }

        public AutomationElement Window { get; }

        public void Dispose()
        {
            try
            {
                if (windowPattern is not null && (launched || originalState == WindowVisualState.Minimized))
                    windowPattern.SetWindowVisualState(WindowVisualState.Minimized);
            }
            catch (ElementNotAvailableException) { }
            catch (InvalidOperationException) { }
        }
    }
}
