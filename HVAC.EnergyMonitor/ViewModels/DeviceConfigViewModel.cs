using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using HVAC.EnergyMonitor.Models.Entities;
using HVAC.EnergyMonitor.Models.Enums;
using HVAC.EnergyMonitor.Infrastructure.Repository;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.ViewModels;

public class DeviceConfigViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private Device? _selectedDevice;

    public ObservableCollection<Device> Devices { get; } = new();

    public IReadOnlyList<ProtocolType> ProtocolTypes { get; } = new List<ProtocolType>
    {
        ProtocolType.Simulator,
        ProtocolType.ModbusTCP,
        ProtocolType.ModbusRTU
    };

    public Device? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public DelegateCommand AddCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    public DeviceConfigViewModel(IUnitOfWork unitOfWork, DialogServiceInterface dialogService)
        : base(dialogService)
    {
        _unitOfWork = unitOfWork;

        AddCommand = CreateAsyncCommand(async () => await AddDeviceAsync(), "新增设备", () => !IsBusy);
        DeleteCommand = CreateAsyncCommand(async () => await DeleteDeviceAsync(), "删除设备", () => !IsBusy && SelectedDevice != null)
            .ObservesProperty(() => SelectedDevice);
        SaveCommand = CreateAsyncCommand(async () => await SaveAsync(), "保存设备", () => !IsBusy);
        RefreshCommand = CreateAsyncCommand(async () => await LoadAsync(), "刷新设备", () => !IsBusy);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(async ct => await LoadAsync(ct), "加载设备配置");
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        var devices = await _unitOfWork.Repository<Device>().GetAllAsync(ct);
        Devices.Clear();
        foreach (var device in devices)
        {
            Devices.Add(device);
        }
        SelectedDevice = Devices.FirstOrDefault();
    }

    private async Task AddDeviceAsync()
    {
        var device = new Device
        {
            Name = "新设备",
            ProtocolType = ProtocolType.Simulator,
            IpAddress = "127.0.0.1",
            Port = 502,
            SlaveAddress = 1,
            ScanIntervalMs = 1000,
            IsEnabled = true
        };

        await _unitOfWork.Repository<Device>().AddAsync(device, CancellationToken);
        await _unitOfWork.SaveChangesAsync();

        Devices.Add(device);
        SelectedDevice = device;
    }

    private async Task DeleteDeviceAsync()
    {
        var device = SelectedDevice;
        if (device == null) return;

        if (!DialogService.Ask($"确定要删除设备 \"{device.Name}\" 吗？"))
        {
            return;
        }

        _unitOfWork.Repository<Device>().Remove(device);
        await _unitOfWork.SaveChangesAsync();

        Devices.Remove(device);
        SelectedDevice = Devices.FirstOrDefault();
    }

    private async Task SaveAsync()
    {
        var errors = Devices
            .OfType<System.ComponentModel.IDataErrorInfo>()
            .SelectMany(d => new[] { d[nameof(Device.Name)], d[nameof(Device.ScanIntervalMs)], d[nameof(Device.Port)], d[nameof(Device.BaudRate)] })
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();

        if (errors.Count > 0)
        {
            DialogService.ShowWarning($"数据验证失败：\n{string.Join("\n", errors.Distinct())}");
            return;
        }

        await _unitOfWork.SaveChangesAsync();
        DialogService.ShowInfo("设备配置已保存");
        await LoadAsync(CancellationToken);
    }

    public override void Dispose()
    {
        _unitOfWork?.Dispose();
        base.Dispose();
    }
}
