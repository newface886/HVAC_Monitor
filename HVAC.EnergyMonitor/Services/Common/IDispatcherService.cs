using System;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Common;

public interface IDispatcherService
{
    Task InvokeAsync(Action action);

    Task<T> InvokeAsync<T>(Func<T> func);
}
