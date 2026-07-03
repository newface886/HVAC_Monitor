using System;
using System.Threading.Tasks;
using System.Windows;

namespace HVAC.EnergyMonitor.Services.Common;

public class DispatcherService : IDispatcherService
{
    public Task InvokeAsync(Action action)
    {
        return Application.Current.Dispatcher.InvokeAsync(action).Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        return Application.Current.Dispatcher.InvokeAsync(func).Task;
    }
}
