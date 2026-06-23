using Prism.Events;

namespace HVAC.EnergyMonitor.Models.Events;

public class PointValueUpdatedEvent : PubSubEvent<int>
{
}
