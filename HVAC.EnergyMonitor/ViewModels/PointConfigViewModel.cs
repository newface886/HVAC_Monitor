using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Models.Entities;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.ViewModels;

public class PointConfigViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private Point? _selectedPoint;

    public ObservableCollection<Point> Points { get; } = new();

    public Point? SelectedPoint
    {
        get => _selectedPoint;
        set => SetProperty(ref _selectedPoint, value);
    }

    public DelegateCommand AddCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    public PointConfigViewModel(IUnitOfWork unitOfWork, DialogServiceInterface dialogService)
        : base(dialogService)
    {
        _unitOfWork = unitOfWork;
        AddCommand = CreateAsyncCommand(async () => await AddPointAsync(), "新增点位", () => !IsBusy);
        SaveCommand = CreateAsyncCommand(async () => await SaveAsync(), "保存点位", () => !IsBusy);
        RefreshCommand = CreateAsyncCommand(async () => await LoadAsync(), "刷新点位", () => !IsBusy);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(async ct => await LoadAsync(ct), "加载点位配置");
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        var points = await _unitOfWork.Repository<Point>().GetAllAsync(ct);
        Points.Clear();
        foreach (var point in points)
        {
            Points.Add(point);
        }
    }

    private async Task AddPointAsync()
    {
        var devices = await _unitOfWork.Repository<Models.Entities.Device>().GetAllAsync(CancellationToken);
        var deviceId = devices.FirstOrDefault()?.Id ?? 0;
        if (deviceId == 0)
        {
            DialogService.ShowWarning("没有可用设备，无法新增点位");
            return;
        }

        var point = new Point
        {
            DeviceId = deviceId,
            Name = "新点位",
            Unit = "°C",
            FunctionCode = 3,
            DataType = Models.Enums.DataType.UShort,
            Scale = 1,
            Offset = 0,
            IsEnabled = true
        };

        await _unitOfWork.Repository<Point>().AddAsync(point, CancellationToken);
        await _unitOfWork.SaveChangesAsync();

        Points.Add(point);
        SelectedPoint = point;
    }

    private async Task SaveAsync()
    {
        var errors = Points
            .OfType<System.ComponentModel.IDataErrorInfo>()
            .SelectMany(p => new[]
            {
                p[nameof(Point.Name)],
                p[nameof(Point.RegisterAddress)],
                p[nameof(Point.FunctionCode)],
                p[nameof(Point.Scale)],
                p[nameof(Point.DeviceId)]
            })
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();

        if (errors.Count > 0)
        {
            DialogService.ShowWarning($"数据验证失败：\n{string.Join("\n", errors.Distinct())}");
            return;
        }

        await _unitOfWork.SaveChangesAsync();
        DialogService.ShowInfo("点位配置已保存");
        await LoadAsync(CancellationToken);
    }

    public override void Dispose()
    {
        _unitOfWork?.Dispose();
        base.Dispose();
    }
}
