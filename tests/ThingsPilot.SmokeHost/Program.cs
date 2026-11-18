using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

internal static class SmokeHost
{
    [DllImport("ole32.dll", PreserveSig = true)]
    private static extern int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);
    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(nint reserved, uint flags);

    [MTAThread]
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass the published ThingsPilot.exe path.");
        using var server = Process.Start(new ProcessStartInfo(Path.GetFullPath(args[0]), "-RegisterProcessAsComServer")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        try
        {
            await Task.Delay(1500);
            Marshal.ThrowExceptionForHR(CoInitializeEx(0, 0));
            var clsid = new Guid("DE91DA32-C49B-4C38-9692-D125C84FE05C");
            var iid = new Guid("00000000-0000-0000-C000-000000000046");
            Marshal.ThrowExceptionForHR(CoCreateInstance(in clsid, 0, 4, in iid, out var pointer));
            IExtension extension;
            try { extension = WinRT.MarshalInterface<IExtension>.FromAbi(pointer); }
            finally { Marshal.Release(pointer); }
            var provider = (ICommandProvider)extension.GetProvider(ProviderType.Commands);
            var host = new FeedbackHost();
            provider.InitializeWithHost(host);
            if (provider.DisplayName != "ThingsPilot") throw new Exception("Wrong provider name.");
            var commands = provider.TopLevelCommands();
            if (commands.Length < 1 || commands[0].Title != "ThingsPilot") throw new Exception("No launch command.");
            if (provider.GetCommand(commands[0].Command.Id) is not IListPage)
                throw new Exception("Saved ThingsPilot page command cannot be resolved.");
            var page = (IListPage)commands[0].Command;
            var items = page.GetItems();
            if (!items.Any(item => item.Title == "Refresh from SmartThings")) throw new Exception("Refresh command missing.");
            if (string.IsNullOrWhiteSpace(items.Single(item => item.Title == "Refresh from SmartThings").Command.Name))
                throw new Exception("Refresh command has no action label.");
            if (!items.Any(item => item.Title == "Open SmartThings")) throw new Exception("Open command missing.");
            var demo = (IListPage)items.Single(item => item.Title == "Try demo routines").Command;
            ((IDynamicListPage)demo).SearchText = "good night";
            var demoItems = demo.GetItems();
            if (demoItems.Count(item => item.Title == "Good Night") != 2 || demoItems.Any(item => item.Title == "Study"))
                throw new Exception("Demo page search did not filter its routine names.");
            if (!demoItems.Any(item => item.Title == "Good Night" && item.Subtitle.Contains("Demo Home")))
                throw new Exception("Demo routine missing.");
            var sample = demoItems.First(item => item.Title == "Good Night");
            ((IInvokableCommand)sample.Command).Invoke(null!);
            var feedback = await host.Feedback.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (!feedback.Contains("Simulated only")) throw new Exception("Demo invocation did not deliver completion feedback.");
            Console.WriteLine($"PASS: Out-of-process COM activation; provider; {commands.Length} commands; routine page; local SmartThings commands.");
            (extension as IDisposable)?.Dispose();
            return 0;
        }
        finally { if (!server.HasExited) server.Kill(); }
    }
}

internal sealed partial class FeedbackHost : IExtensionHost
{
    public TaskCompletionSource<string> Feedback { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Windows.Foundation.IAsyncAction ShowStatus(IStatusMessage message, StatusContext context)
    {
        Feedback.TrySetResult(message.Message);
        return Task.CompletedTask.AsAsyncAction();
    }
    public Windows.Foundation.IAsyncAction HideStatus(IStatusMessage message) => Task.CompletedTask.AsAsyncAction();
    public Windows.Foundation.IAsyncAction LogMessage(ILogMessage message) => Task.CompletedTask.AsAsyncAction();
}
