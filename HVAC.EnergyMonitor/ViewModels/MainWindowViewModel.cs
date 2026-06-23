using Prism.Commands;
using Prism.Navigation.Regions;
using System;

namespace HVAC.EnergyMonitor.ViewModels;

public class MainWindowViewModel
{
    private readonly IRegionManager _regionManager;

    public DelegateCommand<string> NavigateCommand { get; }

    public MainWindowViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
        NavigateCommand = new DelegateCommand<string>(Navigate);

        Navigate("DashboardView");
    }

    private void Navigate(string viewName)
    {
        if (string.IsNullOrEmpty(viewName)) return;
        _regionManager.RequestNavigate("MainRegion", viewName);
    }
}
