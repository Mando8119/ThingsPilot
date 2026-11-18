using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

namespace ThingsPilot.Extension;

[Guid("DE91DA32-C49B-4C38-9692-D125C84FE05C")]
public sealed partial class ThingsPilotExtension(ManualResetEvent disposed) : IExtension, IDisposable
{
    private readonly ThingsPilotProvider provider = new();
    public object? GetProvider(ProviderType type) => type == ProviderType.Commands ? provider : null;
    public void Dispose() { provider.Dispose(); disposed.Set(); }
}
