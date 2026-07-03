using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Models.DTOs;
using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Services.Common;
using HVAC.EnergyMonitor.Services.Report;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HVAC.EnergyMonitor.ViewModels;

public class EnergyReportViewModel : ViewModelBase
{
    private readonly IEnergyReportService _reportService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDispatcherService _dispatcher;
    private int _selectedPointId;
    private string _selectedPeriod = "Hour";

    public ObservableCollection<Models.Entities.Point> Points { get; } = new();
    public ObservableCollection<EnergyReportDto> Reports { get; } = new();

    public int SelectedPointId
    {
        get => _selectedPointId;
        set => SetProperty(ref _selectedPointId, value);
    }

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set => SetProperty(ref _selectedPeriod, value);
    }

    public DelegateCommand QueryCommand { get; }
    public DelegateCommand ExportCommand { get; }

    public EnergyReportViewModel(
        IEnergyReportService reportService,
        IUnitOfWork unitOfWork,
        DialogServiceInterface dialogService,
        IDispatcherService dispatcher)
        : base(dialogService)
    {
        _reportService = reportService;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        QueryCommand = CreateAsyncCommand(async () => await QueryAsync(), "查询能耗报表", () => !IsBusy);
        ExportCommand = CreateAsyncCommand(async () => await ExportAsync(), "导出能耗报表", () => !IsBusy);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(async ct => await LoadPointsAsync(ct), "加载点位");
    }

    private async Task LoadPointsAsync(CancellationToken ct = default)
    {
        var points = await _unitOfWork.Repository<Models.Entities.Point>().GetAllAsync(ct);
        Points.Clear();
        foreach (var point in points)
        {
            Points.Add(point);
        }
        if (Points.Any()) SelectedPointId = Points.First().Id;
    }

    private async Task QueryAsync()
    {
        var start = DateTime.Now.AddDays(-1);
        var end = DateTime.Now;

        IEnumerable<EnergyReportDto> result = SelectedPeriod switch
        {
            "Day" => await _reportService.GetDailyReportAsync(SelectedPointId, start, end),
            "Month" => await _reportService.GetMonthlyReportAsync(SelectedPointId, start, end),
            _ => await _reportService.GetHourlyReportAsync(SelectedPointId, start, end)
        };

        await _dispatcher.InvokeAsync(() =>
        {
            Reports.Clear();
            foreach (var r in result)
            {
                Reports.Add(r);
            }
            RaisePropertyChanged(nameof(Reports));
        });
    }

    private async Task ExportAsync()
    {
        if (Reports.Count == 0)
        {
            DialogService.ShowWarning("没有可导出的报表数据");
            return;
        }

        var fileName = DialogService.ShowSaveFileDialog("CSV 文件|*.csv", $"EnergyReport_{DateTime.Now:yyyyMMddHHmmss}.csv");
        if (string.IsNullOrEmpty(fileName)) return;

        var sb = new StringBuilder();
        sb.AppendLine("周期开始,周期结束,周期类型,累计值,单位");
        foreach (var r in Reports)
        {
            sb.AppendLine($"{r.PeriodStart:yyyy-MM-dd HH:mm:ss},{r.PeriodEnd:yyyy-MM-dd HH:mm:ss},{r.PeriodType},{r.TotalValue:F2},{r.Unit}");
        }

        await File.WriteAllTextAsync(fileName, sb.ToString(), Encoding.UTF8);
        DialogService.ShowInfo($"报表已导出到：{fileName}");
    }

    public override void Dispose()
    {
        _unitOfWork?.Dispose();
        base.Dispose();
    }
}
