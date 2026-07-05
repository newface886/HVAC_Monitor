using HVAC.EnergyMonitor.Models;
using HVAC.EnergyMonitor.ViewModels;
using ScottPlot;
using ScottPlot.DataSources;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HVAC.EnergyMonitor.Views;

public partial class DashboardView : UserControl
{
    private readonly List<double> _chillerXs = new();
    private readonly List<double> _chillerYs = new();
    private readonly List<double> _otherXs = new();
    private readonly List<double> _otherYs = new();

    private ScottPlot.Plottables.SignalXY? _chillerSignal;
    private ScottPlot.Plottables.SignalXY? _otherSignal;

    private System.Windows.Threading.DispatcherTimer? _plotTimer;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm) return;

        var plot = RealtimePlot.Plot;
        plot.Clear();
        plot.Axes.DateTimeTicksBottom();
        plot.Title("冷水机组功率实时趋势（默认）");
        plot.XLabel("时间");
        plot.YLabel("功率 (kW)");

        FillList(_chillerXs, _chillerYs, vm.ChillerPowerTrend);
        FillList(_otherXs, _otherYs, vm.OtherTrends);

        // 默认只显示冷水机组功率曲线
        if (_chillerXs.Count > 0)
        {
            _chillerSignal = CreateSignal(plot, _chillerXs, _chillerYs, new ScottPlot.Color(0, 102, 204), 3, "冷机功率");
        }

        // 其他曲线默认较细/半透明，避免干扰
        if (_otherXs.Count > 0)
        {
            _otherSignal = CreateSignal(plot, _otherXs, _otherYs, new ScottPlot.Color(180, 180, 180), 1, "其他点位");
        }

        plot.ShowLegend();
        plot.Axes.AutoScale();

        _plotTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
        _plotTimer.Tick += (s, args) =>
        {
            FillList(_chillerXs, _chillerYs, vm.ChillerPowerTrend);
            FillList(_otherXs, _otherYs, vm.OtherTrends);

            UpdateSignal(ref _chillerSignal, plot, _chillerXs, _chillerYs, new ScottPlot.Color(0, 102, 204), 3, "冷机功率");
            UpdateSignal(ref _otherSignal, plot, _otherXs, _otherYs, new ScottPlot.Color(180, 180, 180), 1, "其他点位");

            plot.Axes.AutoScale();
            RealtimePlot.Refresh();
        };
        _plotTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _plotTimer?.Stop();
        // ViewModel 生命周期由 Prism Region + INavigationAware 管理，View 不再 Dispose
    }

    private ScottPlot.Plottables.SignalXY CreateSignal(ScottPlot.Plot plot, List<double> xs, List<double> ys, ScottPlot.Color color, float lineWidth, string legendText)
    {
        var source = new SignalXYSourceDoubleArray(xs.ToArray(), ys.ToArray());
        var signal = plot.Add.SignalXY(source);
        signal.Color = color;
        signal.LineWidth = lineWidth;
        signal.LegendText = legendText;
        return signal;
    }

    private void UpdateSignal(ref ScottPlot.Plottables.SignalXY? signal, ScottPlot.Plot plot, List<double> xs, List<double> ys, ScottPlot.Color color, float lineWidth, string legendText)
    {
        if (xs.Count == 0)
            return;

        var source = new SignalXYSourceDoubleArray(xs.ToArray(), ys.ToArray());
        if (signal is null)
        {
            signal = plot.Add.SignalXY(source);
            signal.Color = color;
            signal.LineWidth = lineWidth;
            signal.LegendText = legendText;
        }
        else
        {
            signal.Data = source;
        }
    }

    private static void FillList(List<double> xs, List<double> ys, System.Collections.ObjectModel.ObservableCollection<HistoryDataPoint> points)
    {
        xs.Clear();
        ys.Clear();
        foreach (var p in points)
        {
            xs.Add(p.Timestamp.ToOADate());
            ys.Add(p.Value);
        }
    }
}
