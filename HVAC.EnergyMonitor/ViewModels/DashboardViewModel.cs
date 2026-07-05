using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Infrastructure.DbContext;
using HVAC.EnergyMonitor.Models;
using HVAC.EnergyMonitor.Models.Enums;
using HVAC.EnergyMonitor.Models.Events;
using HVAC.EnergyMonitor.Services.Acquisition;
using HVAC.EnergyMonitor.Services.Cache;
using HVAC.EnergyMonitor.Services.Common;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HVAC.EnergyMonitor.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IPointValueCache _cache;
    private readonly IDataAcquisitionService _acquisition;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IDispatcherService _dispatcher;
    private readonly Timer _refreshTimer;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMilliseconds(500);
    private readonly Prism.Events.SubscriptionToken _pointValueUpdatedSubscription;

    public ObservableCollection<PointDisplayItem> PointValues { get; } = new();
    public ObservableCollection<HistoryDataPoint> ChillerPowerTrend { get; } = new();
    public ObservableCollection<HistoryDataPoint> OtherTrends { get; } = new();

    public string AcquisitionStatus => _acquisition.IsRunning ? "运行中" : "已停止";
    public string OnlineStatus => _acquisition.IsRunning ? "在线" : "离线";

    public DashboardViewModel(
        IPointValueCache cache,
        IDataAcquisitionService acquisition,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IEventAggregator eventAggregator,
        DialogServiceInterface dialogService,
        IDispatcherService dispatcher)
        : base(dialogService)
    {
        _cache = cache;
        _acquisition = acquisition;
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;

        _pointValueUpdatedSubscription = eventAggregator.GetEvent<PointValueUpdatedEvent>().Subscribe(_ =>
            _refreshTimer?.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan));

        _refreshTimer = new Timer(
            async _ => await RefreshAsync(),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            await _acquisition.StartAsync();
            await RefreshAsync();
        }, "初始化实时监控");
    }

    private async Task RefreshAsync()
    {
        if (IsDisposed) return;
        if (!await _refreshLock.WaitAsync(0)) return;

        try
        {
            List<PointDisplayItem> items;
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var points = await context.Points.AsNoTracking().ToListAsync();
                var allValues = _cache.GetAllValues();

                items = points.Select(point =>
                {
                    var cacheItem = allValues.TryGetValue(point.Id, out var item) ? item : null;
                    return new PointDisplayItem
                    {
                        Name = point.Name,
                        Value = cacheItem?.Value ?? 0,
                        Unit = point.Unit,
                        Timestamp = cacheItem?.Timestamp ?? DateTime.MinValue,
                        Quality = cacheItem?.Quality ?? Quality.NotConnected
                    };
                }).ToList();
            }

            await _dispatcher.InvokeAsync(() =>
            {
                if (IsDisposed) return;

                PointValues.Clear();
                foreach (var item in items)
                {
                    PointValues.Add(item);
                }

                AppendTrendData();
                RaisePropertyChanged(nameof(AcquisitionStatus));
                RaisePropertyChanged(nameof(OnlineStatus));
            });
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        finally
        {
            _refreshLock.Release();
            _refreshTimer?.Change(_refreshInterval, Timeout.InfiniteTimeSpan);
        }
    }

    private void AppendTrendData()
    {
        var now = DateTime.Now;
        foreach (var point in PointValues)
        {
            var item = new HistoryDataPoint { Timestamp = now, Value = point.Value };
            if (point.Name == "冷机功率")
            {
                ChillerPowerTrend.Add(item);
                while (ChillerPowerTrend.Count > 300) ChillerPowerTrend.RemoveAt(0);
            }
            else
            {
                OtherTrends.Add(item);
                while (OtherTrends.Count > 300) OtherTrends.RemoveAt(0);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _pointValueUpdatedSubscription?.Dispose();
        _refreshTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _refreshTimer?.Dispose();
        _refreshLock.Dispose();
        _ = _acquisition.StopAsync();
    }
}
