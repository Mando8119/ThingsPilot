// Copyright (c) Microsoft Corporation. Licensed under the MIT license.
// COM hosting adapted from the Command Palette template; see THIRD-PARTY-NOTICES.md.
using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace ThingsPilot.Extension;

internal static class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.FirstOrDefault() != "-RegisterProcessAsComServer")
        {
            System.Windows.MessageBox.Show(
                "ThingsPilot is a PowerToys Command Palette extension.\n\nOpen Command Palette with Win+Alt+Space, select ThingsPilot → Open routines, then Refresh from SmartThings.\n\nIf it is missing, reload extensions in Command Palette Settings → Extensions → Installed → More options.",
                "Open ThingsPilot in Command Palette", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        using var disposed = new ManualResetEvent(false);
        var extension = new ThingsPilotExtension(disposed);
        var server = new ComServer();
        server.RegisterClass<ThingsPilotExtension, IExtension>(() => extension);
        server.Start();
        disposed.WaitOne();
        server.Stop();
        server.UnsafeDispose();
    }
}
