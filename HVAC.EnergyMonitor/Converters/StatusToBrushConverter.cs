using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HVAC.EnergyMonitor.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public Brush OnlineBrush { get; set; } = Brushes.Green;
    public Brush OfflineBrush { get; set; } = Brushes.Gray;
    public Brush AlarmBrush { get; set; } = Brushes.Red;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();
        return status switch
        {
            "true" or "online" or "运行中" => OnlineBrush,
            "alarm" or "danger" => AlarmBrush,
            _ => OfflineBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
