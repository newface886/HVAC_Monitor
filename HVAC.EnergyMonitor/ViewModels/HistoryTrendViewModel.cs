using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Models;
using HVAC.EnergyMonitor.Models.Entities;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.ViewModels;

public class HistoryTrendViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private int _selectedPointId;
    private DateTime _startTime = DateTime.Now.AddDays(-1);
    private DateTime _endTime = DateTime.Now;

    public ObservableCollection<Point> Points { get; } = new();
    public ObservableCollection<HistoryDataPoint> DataPoints { get; } = new();
    public ObservableCollection<HistoryDataPoint> HourlyDataPoints { get; } = new();
    public ObservableCollection<HistoryDataPoint> DailyDataPoints { get; } = new();
    public ObservableCollection<HistoryDataPoint> MonthlyDataPoints { get; } = new();

    public int SelectedPointId
    {
        get => _selectedPointId;
        set => SetProperty(ref _selectedPointId, value);
    }

    public DateTime StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public DateTime EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public DelegateCommand QueryCommand { get; }
    public DelegateCommand ExportCommand { get; }

    public HistoryTrendViewModel(IUnitOfWork unitOfWork, DialogServiceInterface dialogService)
        : base(dialogService)
    {
        _unitOfWork = unitOfWork;
        QueryCommand = CreateAsyncCommand(async () => await QueryAsync(), "查询历史趋势", () => !IsBusy);
        ExportCommand = CreateAsyncCommand(async () => await ExportAsync(), "导出历史趋势", () => !IsBusy);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(async ct => await LoadPointsAsync(ct), "加载点位");
    }

    private async Task LoadPointsAsync(CancellationToken ct = default)
    {
        var points = await _unitOfWork.Repository<Point>().GetAllAsync(ct);
        Points.Clear();
        foreach (var point in points)
        {
            Points.Add(point);
        }
        if (Points.Any()) SelectedPointId = Points.First().Id;
    }

    private async Task QueryAsync()
    {
        var values = await _unitOfWork.Repository<PointValue>().FindAsync(
            v => v.PointId == SelectedPointId && v.Timestamp >= StartTime && v.Timestamp <= EndTime,
            CancellationToken);

        var sorted = values.OrderBy(v => v.Timestamp).ToList();
        DataPoints.Clear();
        foreach (var v in sorted)
        {
            DataPoints.Add(new HistoryDataPoint { Timestamp = v.Timestamp, Value = v.Value });
        }

        AggregateDataPoints();
        RaisePropertyChanged(nameof(DataPoints));
    }

    private void AggregateDataPoints()
    {
        var source = DataPoints.ToList();

        FillCollection(HourlyDataPoints, source);

        var daily = source
            .GroupBy(p => p.Timestamp.Date)
            .Select(g => new HistoryDataPoint { Timestamp = g.Key, Value = g.Average(p => p.Value) })
            .OrderBy(p => p.Timestamp)
            .ToList();
        FillCollection(DailyDataPoints, daily);

        var monthly = source
            .GroupBy(p => new DateTime(p.Timestamp.Year, p.Timestamp.Month, 1))
            .Select(g => new HistoryDataPoint { Timestamp = g.Key, Value = g.Average(p => p.Value) })
            .OrderBy(p => p.Timestamp)
            .ToList();
        FillCollection(MonthlyDataPoints, monthly);
    }

    private static void FillCollection(ObservableCollection<HistoryDataPoint> collection, List<HistoryDataPoint> source)
    {
        collection.Clear();
        foreach (var p in source)
            collection.Add(p);
    }

    private async Task ExportAsync()
    {
        if (DataPoints.Count == 0)
        {
            DialogService.ShowWarning("没有可导出的历史数据");
            return;
        }

        var fileName = DialogService.ShowSaveFileDialog("CSV 文件|*.csv", $"HistoryTrend_{DateTime.Now:yyyyMMddHHmmss}.csv");
        if (string.IsNullOrEmpty(fileName)) return;

        var sb = new StringBuilder();
        sb.AppendLine("时间,值");
        foreach (var p in DataPoints.OrderBy(p => p.Timestamp))
        {
            sb.AppendLine($"{p.Timestamp:yyyy-MM-dd HH:mm:ss},{p.Value:F2}");
        }

        await File.WriteAllTextAsync(fileName, sb.ToString(), Encoding.UTF8);
        DialogService.ShowInfo($"历史趋势已导出到：{fileName}");
    }

    public override void Dispose()
    {
        _unitOfWork?.Dispose();
        base.Dispose();
    }
}
