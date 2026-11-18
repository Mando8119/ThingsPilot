using System.Runtime.InteropServices;
using System.Windows.Automation;
using ThingPilot.Core;

namespace ThingPilot.Extension;

internal static class PlayActivation
{
    internal static void Activate(AutomationElement window, AutomationElement play, CancellationToken cancellationToken)
    {
        var handle = (nint)window.Current.NativeWindowHandle;
        if (handle == 0) throw new BridgeFailure("SmartThings has no active desktop window. Reopen it and try again.");
        // Do not move focus while the user's Enter from Command Palette is still held down.
        // Its eventual key-up could otherwise activate Play in addition to our own key press.
        var releaseDeadline = Environment.TickCount64 + 1500;
        var activationKeys = new[] { 0x0D, 0x10, 0x11, 0x12, 0x5B, 0x5C };
        while (activationKeys.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Environment.TickCount64 >= releaseDeadline)
                throw new BridgeFailure("Release Enter and modifier keys before running the routine. Nothing was activated.");
            Thread.Sleep(20);
        }
        SetForegroundWindow(handle);
        play.SetFocus();
        cancellationToken.ThrowIfCancellationRequested();
        // UWP can give focus to its CoreWindow rather than ApplicationFrameHost. Compare the root HWNDs.
        if (!play.Current.HasKeyboardFocus || GetAncestor(GetForegroundWindow(), 2) != GetAncestor(handle, 2))
            throw new BridgeFailure("SmartThings could not take focus. Close any dialog and run the command again. Nothing was activated.");
        if (new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0))
            throw new BridgeFailure("Release modifier keys before running the routine. Nothing was activated.");

        // Samsung's InvokePattern focuses this control without executing it. Send precisely one Enter press
        // to the verified Play button instead. Never follow this with Invoke or an automatic retry.
        var inputs = new[] {
            new Input { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { Key = 0x0D } } },
            new Input { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { Key = 0x0D, Flags = 2 } } }
        };
        if (SendInput(2, inputs, Marshal.SizeOf<Input>()) != 2)
            throw new BridgeFailure("Play activation could not be confirmed. Check SmartThings before trying again.");
        // Let the app process the input before a previously minimized window is minimized again.
        // This is a dispatch grace period, not a routine-completion check.
        Thread.Sleep(600);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput { public int X, Y; public uint MouseData, Flags, Time; public nuint Extra; }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint handle, uint flags);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);
}
