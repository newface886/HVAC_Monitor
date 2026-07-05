using System.Windows;

namespace HVAC.EnergyMonitor.Services.Dialog;

public class DialogService : IDialogService
{
    public void ShowError(string message)
    {
        Invoke(() => MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error));
    }

    public void ShowInfo(string message)
    {
        Invoke(() => MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information));
    }

    public void ShowWarning(string message)
    {
        Invoke(() => MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning));
    }

    public bool Ask(string message)
    {
        return Invoke(() => MessageBox.Show(message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
    }

    public string? ShowSaveFileDialog(string filter, string defaultFileName)
    {
        return Invoke(() =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    private static T Invoke<T>(Func<T> func)
    {
        if (Application.Current?.Dispatcher?.CheckAccess() == false)
        {
            return Application.Current.Dispatcher.Invoke(func);
        }
        return func();
    }

    private static void Invoke(Action action)
    {
        if (Application.Current?.Dispatcher?.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(action);
            return;
        }
        action();
    }
}
