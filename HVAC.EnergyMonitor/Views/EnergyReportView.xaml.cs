using HVAC.EnergyMonitor.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HVAC.EnergyMonitor.Views;

public partial class EnergyReportView : UserControl
{
    private EnergyReportViewModel? _vm;
    private PropertyChangedEventHandler? _propertyChangedHandler;

    public EnergyReportView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not EnergyReportViewModel vm) return;
        _vm = vm;
        _propertyChangedHandler = (s, args) =>
        {
            if (args.PropertyName == nameof(EnergyReportViewModel.Reports))
                RefreshPie(vm);
        };
        vm.PropertyChanged += _propertyChangedHandler;
        RefreshPie(vm);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null && _propertyChangedHandler != null)
        {
            _vm.PropertyChanged -= _propertyChangedHandler;
            _vm = null;
            _propertyChangedHandler = null;
        }
        // ViewModel 生命周期由 Prism Region + INavigationAware 管理，View 不再 Dispose
    }

    private void RefreshPie(EnergyReportViewModel vm)
    {
        var plot = EnergyPiePlot.Plot;
        plot.Clear();

        var values = vm.Reports.Select(r => r.TotalValue).ToArray();
        if (values.Length > 0)
        {
            var labels = vm.Reports.Select(r => r.PeriodStart.ToString("MM-dd HH")).ToArray();
            var slices = values.Select((v, i) => new ScottPlot.PieSlice { Value = v, Label = labels[i] }).ToList();
            var pie = plot.Add.Pie(slices);
            pie.ShowSliceLabels = true;
        }

        EnergyPiePlot.Refresh();
    }
}
