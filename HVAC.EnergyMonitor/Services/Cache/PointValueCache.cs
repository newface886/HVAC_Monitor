using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HVAC.EnergyMonitor.Services.Cache;

public class PointValueCache : IPointValueCache
{
    private readonly ConcurrentDictionary<int, PointValueCacheItem> _values = new();

    public void SetValue(PointValueCacheItem item)
    {
        _values[item.PointId] = item;
    }

    public PointValueCacheItem? GetValue(int pointId)
    {
        _values.TryGetValue(pointId, out var item);
        return item;
    }

    public IReadOnlyDictionary<int, PointValueCacheItem> GetAllValues()
    {
        return _values.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
