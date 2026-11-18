using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

namespace ThingPilot.Extension;

[Guid("DE91DA32-C49B-4C38-9692-D125C84FE05C")]
public sealed partial class ThingPilotExtension(ManualResetEvent disposed) : IExtension, IDisposable
{
    private readonly ThingPilotProvider provider = new();
    public object? GetProvider(ProviderType type) => type == ProviderType.Commands ? provider : null;
    public void Dispose() { provider.Dispose(); disposed.Set(); }
}
