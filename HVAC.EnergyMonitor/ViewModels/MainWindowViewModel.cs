using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Constants;
using Prism.Commands;
using Prism.Navigation.Regions;
using System;
using System.Windows.Threading;

namespace HVAC.EnergyMonitor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IRegionManager _regionManager;
    private readonly DispatcherTimer _timer;
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private string _selectedViewName = NavigationKeys.Dashboard;

    public string CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    public string SelectedViewName
    {
        get => _selectedViewName;
        set => SetProperty(ref _selectedViewName, value);
    }

    public DelegateCommand<string> NavigateCommand { get; }

    public MainWindowViewModel(IRegionManager regionManager, DialogServiceInterface dialogService)
        : base(dialogService)
    {
        _regionManager = regionManager;
        NavigateCommand = new DelegateCommand<string>(Navigate, _ => !IsDisposed)
            .ObservesProperty(() => IsBusy);
        Navigate(NavigationKeys.Dashboard);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void Navigate(string viewName)
    {
        if (string.IsNullOrEmpty(viewName) || IsDisposed) return;
        _regionManager.RequestNavigate(RegionNames.MainRegion, viewName);
        SelectedViewName = viewName;
    }

    public override void Dispose()
    {
        base.Dispose();
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
