using HVAC.EnergyMonitor.Services.Acquisition;
using HVAC.EnergyMonitor.Services.Storage;
using NLog;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HVAC.EnergyMonitor;

public partial class App : Application
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private Bootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LogManager.Setup().LoadConfigurationFromFile("NLog.config", optional: true);

        Logger.Info("[App] OnStartup 开始");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            Logger.Info("[App] 创建 Bootstrapper");
            _bootstrapper = new Bootstrapper();
            Logger.Info("[App] 启动 Bootstrapper.Run()");
            _bootstrapper.Run();
            Logger.Info("[App] Bootstrapper.Run() 完成，主窗口应已显示");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "[App] 启动失败: {Message}", ex.Message);
            MessageBox.Show($"启动失败：{ex.Message}", "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_bootstrapper?.Container.Resolve<IDataAcquisitionService>() is IAsyncDisposable acquisitionAsync)
            {
                await acquisitionAsync.DisposeAsync();
            }
            if (_bootstrapper?.Container.Resolve<IDataStorageService>() is IAsyncDisposable storageAsync)
            {
                await storageAsync.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "应用退出清理资源时发生异常");
        }
        finally
        {
            LogManager.Shutdown();
            base.OnExit(e);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Fatal(e.Exception, "UI 线程未处理异常: {Message}", e.Exception.Message);
        MessageBox.Show($"发生未处理异常：{e.Exception.Message}", "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error(e.Exception, "Task 未观察异常");
        e.SetObserved();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.Fatal(ex, "AppDomain 未处理异常: {Message}", ex.Message);
        }
    }
}
