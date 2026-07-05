using HVAC.EnergyMonitor.Models;
using HVAC.EnergyMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace HVAC.EnergyMonitor.Views;

public partial class HistoryTrendView : UserControl
{
    private ScottPlot.Plottables.Scatter? _hourScatter;
    private ScottPlot.Plottables.Scatter? _dayScatter;
    private ScottPlot.Plottables.Scatter? _monthScatter;

    private readonly List<double> _hourXs = new();
    private readonly List<double> _hourYs = new();
    private readonly List<double> _dayXs = new();
    private readonly List<double> _dayYs = new();
    private readonly List<double> _monthXs = new();
    private readonly List<double> _monthYs = new();

    public HistoryTrendView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HistoryTrendViewModel vm)
        {
            vm.HourlyDataPoints.CollectionChanged -= OnDataCollectionChanged;
            vm.DailyDataPoints.CollectionChanged -= OnDataCollectionChanged;
            vm.MonthlyDataPoints.CollectionChanged -= OnDataCollectionChanged;
        }
        // ViewModel 生命周期由 Prism Region + INavigationAware 管理，View 不再 Dispose
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitPlot(HourPlot, "小时趋势", _hourXs, _hourYs, out _hourScatter);
        InitPlot(DayPlot, "日趋势", _dayXs, _dayYs, out _dayScatter);
        InitPlot(MonthPlot, "月趋势", _monthXs, _monthYs, out _monthScatter);
        RefreshPlots();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is HistoryTrendViewModel oldVm)
        {
            oldVm.HourlyDataPoints.CollectionChanged -= OnDataCollectionChanged;
            oldVm.DailyDataPoints.CollectionChanged -= OnDataCollectionChanged;
            oldVm.MonthlyDataPoints.CollectionChanged -= OnDataCollectionChanged;
        }

        if (e.NewValue is HistoryTrendViewModel newVm)
        {
            newVm.HourlyDataPoints.CollectionChanged += OnDataCollectionChanged;
            newVm.DailyDataPoints.CollectionChanged += OnDataCollectionChanged;
            newVm.MonthlyDataPoints.CollectionChanged += OnDataCollectionChanged;
        }
    }

    private void OnDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.Invoke(RefreshPlots);
    }

    private void RefreshPlots()
    {
        if (DataContext is not HistoryTrendViewModel vm) return;

        FillList(_hourXs, _hourYs, vm.HourlyDataPoints);
        FillList(_dayXs, _dayYs, vm.DailyDataPoints);
        FillList(_monthXs, _monthYs, vm.MonthlyDataPoints);

        HourPlot.Refresh();
        DayPlot.Refresh();
        MonthPlot.Refresh();
    }

    private static void InitPlot(ScottPlot.WPF.WpfPlot plot, string title, List<double> xs, List<double> ys, out ScottPlot.Plottables.Scatter scatter)
    {
        plot.Plot.Clear();
        plot.Plot.Axes.DateTimeTicksBottom();
        plot.Plot.Title(title);
        scatter = plot.Plot.Add.Scatter(xs, ys);
        scatter.Color = new ScottPlot.Color(0, 102, 204);
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
