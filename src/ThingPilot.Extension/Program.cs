// Copyright (c) Microsoft Corporation. Licensed under the MIT license.
// COM hosting adapted from the Command Palette template; see THIRD-PARTY-NOTICES.md.
using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace ThingPilot.Extension;

internal static class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.FirstOrDefault() != "-RegisterProcessAsComServer")
        {
            System.Windows.MessageBox.Show(
                "ThingPilot is a PowerToys Command Palette extension.\n\nOpen Command Palette with Win+Alt+Space, select ThingPilot → Open routines, then Refresh from SmartThings.\n\nIf it is missing, reload extensions in Command Palette Settings → Extensions → Installed → More options.",
                "Open ThingPilot in Command Palette", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        using var disposed = new ManualResetEvent(false);
        var extension = new ThingPilotExtension(disposed);
        var server = new ComServer();
        server.RegisterClass<ThingPilotExtension, IExtension>(() => extension);
        server.Start();
        disposed.WaitOne();
        server.Stop();
        server.UnsafeDispose();
    }
}
