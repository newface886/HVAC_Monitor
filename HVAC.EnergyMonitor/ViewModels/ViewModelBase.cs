using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;
using NLog;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.ViewModels;

public abstract class ViewModelBase : BindableBase, IDisposable, IRegionMemberLifetime, INavigationAware
{
    private readonly DialogServiceInterface _dialogService;
    private readonly CancellationTokenSource _cts = new();
    private bool _isBusy;
    private string? _errorMessage;
    private bool _disposed;

    protected ViewModelBase(DialogServiceInterface dialogService)
    {
        _dialogService = dialogService;
        Logger = LogManager.GetCurrentClassLogger();
    }

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        protected set => SetProperty(ref _errorMessage, value);
    }

    protected ILogger Logger { get; }

    protected DialogServiceInterface DialogService => _dialogService;

    protected bool IsDisposed => _disposed;

    protected CancellationToken CancellationToken => _cts.Token;

    protected async Task ExecuteAsync(Func<Task> action, string operationName)
    {
        if (_disposed) return;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不处理
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[{Operation}] {Message}", operationName, ex.Message);
            ErrorMessage = $"[{operationName}] {ex.Message}";
            _dialogService.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task ExecuteAsync(Func<CancellationToken, Task> action, string operationName)
    {
        if (_disposed) return;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不处理
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[{Operation}] {Message}", operationName, ex.Message);
            ErrorMessage = $"[{operationName}] {ex.Message}";
            _dialogService.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string operationName)
    {
        if (_disposed) return default;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[{Operation}] {Message}", operationName, ex.Message);
            ErrorMessage = $"[{operationName}] {ex.Message}";
            _dialogService.ShowError(ErrorMessage);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, string operationName)
    {
        if (_disposed) return default;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            return await action(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[{Operation}] {Message}", operationName, ex.Message);
            ErrorMessage = $"[{operationName}] {ex.Message}";
            _dialogService.ShowError(ErrorMessage);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected DelegateCommand CreateAsyncCommand(Func<Task> execute, string operationName, Func<bool>? canExecute = null)
    {
        var command = new DelegateCommand(
            async () => await ExecuteAsync(execute, operationName),
            () => !_disposed && (canExecute?.Invoke() ?? true));

        return command.ObservesProperty(() => IsBusy);
    }

    protected DelegateCommand CreateAsyncCommand(Func<CancellationToken, Task> execute, string operationName, Func<bool>? canExecute = null)
    {
        var command = new DelegateCommand(
            async () => await ExecuteAsync(execute, operationName),
            () => !_disposed && (canExecute?.Invoke() ?? true));

        return command.ObservesProperty(() => IsBusy);
    }

    protected DelegateCommand<T> CreateAsyncCommand<T>(Func<T, Task> execute, string operationName, Func<T, bool>? canExecute = null)
    {
        var command = new DelegateCommand<T>(
            async param => await ExecuteAsync(() => execute(param), operationName),
            _ => !_disposed && (canExecute?.Invoke(_) ?? true));

        return command.ObservesProperty(() => IsBusy);
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // 已释放，忽略
        }
    }

    public virtual bool KeepAlive => false;

    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public virtual void OnNavigatedTo(NavigationContext navigationContext) { }

    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Dispose();
    }
}
