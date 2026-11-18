using ThingsPilot.Core;
using ThingsPilot.Extension;

namespace ThingsPilot.Tests;

public sealed class CacheTests
{
    [Fact]
    public void CorruptOrIncompleteCacheCannotProduceExecutableCommands()
    {
        var path = Path.Combine(Path.GetTempPath(), "thingspilot-cache-" + Guid.NewGuid() + ".json");
        try
        {
            var cache = new RoutineCache(path);
            File.WriteAllText(path, "[null,{}, {\"name\":\"\",\"locationName\":\"Home\"}]");
            Assert.Empty(cache.Load());
            File.WriteAllText(path, "broken json");
            Assert.Empty(cache.Load());
            var routine = Routine.Create("Study", "Home");
            cache.Save([routine]);
            Assert.Equal(routine, Assert.Single(cache.Load()));
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
}
