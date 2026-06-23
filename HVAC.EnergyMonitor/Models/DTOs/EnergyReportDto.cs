using System;

namespace HVAC.EnergyMonitor.Models.DTOs;

public class EnergyReportDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public double TotalValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}
