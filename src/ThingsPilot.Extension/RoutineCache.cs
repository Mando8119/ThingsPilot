using System.Text.Json;
using ThingsPilot.Core;

namespace ThingsPilot.Extension;

internal sealed class RoutineCache(string? cachePath = null)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly string path = cachePath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThingsPilot", "routines.json");

    public Routine[] Load()
    {
        try
        {
            if (new FileInfo(path).Length > 2_000_000) return [];
            var entries = JsonSerializer.Deserialize<Routine?[]>(File.ReadAllText(path), Options) ?? [];
            return entries.Where(routine => routine is not null && !string.IsNullOrWhiteSpace(routine.Name)
                && !string.IsNullOrWhiteSpace(routine.LocationName))
                .Select(routine => Routine.Create(routine!.Name, routine.LocationName))
                .DistinctBy(routine => routine.Id).Take(2000).ToArray();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) { return []; }
    }

    public void Save(Routine[] routines)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(routines, Options));
            File.Move(temporary, path, true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
}
