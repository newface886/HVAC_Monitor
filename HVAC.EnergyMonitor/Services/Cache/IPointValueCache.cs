using HVAC.EnergyMonitor.Models.Enums;
using System;
using System.Collections.Generic;

namespace HVAC.EnergyMonitor.Services.Cache;

public record PointValueCacheItem(int PointId, double Value, DateTime Timestamp, Quality Quality);

public interface IPointValueCache
{
    void SetValue(PointValueCacheItem item);
    PointValueCacheItem? GetValue(int pointId);
    IReadOnlyDictionary<int, PointValueCacheItem> GetAllValues();
}
