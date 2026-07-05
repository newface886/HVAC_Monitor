namespace HVAC.EnergyMonitor.Services.Dialog;

public interface IDialogService
{
    void ShowError(string message);
    void ShowInfo(string message);
    void ShowWarning(string message);
    bool Ask(string message);
    string? ShowSaveFileDialog(string filter, string defaultFileName);
}
