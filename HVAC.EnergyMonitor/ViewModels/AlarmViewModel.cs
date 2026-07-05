using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Services.Alarm;
using HVAC.EnergyMonitor.Services.Common;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HVAC.EnergyMonitor.ViewModels;

public class AlarmViewModel : ViewModelBase
{
    private readonly IAlarmService _alarmService;
    private readonly IDispatcherService _dispatcher;
    private readonly DispatcherTimer _refreshTimer;
    private readonly EventHandler<AlarmEventArgs> _alarmTriggeredHandler;

    public ObservableCollection<AlarmRecord> Alarms { get; } = new();

    public DelegateCommand<int?> AcknowledgeCommand { get; }
    public DelegateCommand QueryCommand { get; }
    public DelegateCommand ExportCommand { get; }

    public AlarmViewModel(IAlarmService alarmService, DialogServiceInterface dialogService, IDispatcherService dispatcher)
        : base(dialogService)
    {
        _alarmService = alarmService;
        _dispatcher = dispatcher;

        AcknowledgeCommand = CreateAsyncCommand<int?>(async id => await AcknowledgeAsync(id ?? 0), "确认报警", _ => !IsBusy);
        QueryCommand = CreateAsyncCommand(async () => await RefreshAsync(), "查询报警", () => !IsBusy);
        ExportCommand = new DelegateCommand(() => dialogService.ShowInfo("导出功能待实现"), () => !IsBusy)
            .ObservesProperty(() => IsBusy);

        _alarmTriggeredHandler = async (s, e) => await RefreshAsync();
        _alarmService.AlarmTriggered += _alarmTriggeredHandler;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += OnRefreshTimerTick;
        _refreshTimer.Start();

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(RefreshAsync, "加载报警");
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[AlarmViewModel] OnRefreshTimerTick failed: {Message}", ex.Message);
        }
    }

    private async Task RefreshAsync()
    {
        var alarms = await _alarmService.GetActiveAlarmsAsync(CancellationToken);
        var list = alarms.ToList();

        await _dispatcher.InvokeAsync(() =>
        {
            Alarms.Clear();
            foreach (var alarm in list)
            {
                Alarms.Add(alarm);
            }
        });
    }

    private async Task AcknowledgeAsync(int id)
    {
        await _alarmService.AcknowledgeAsync(id, CancellationToken);
        await RefreshAsync();
    }

    public override void Dispose()
    {
        base.Dispose();
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        _alarmService.AlarmTriggered -= _alarmTriggeredHandler;
    }
}
