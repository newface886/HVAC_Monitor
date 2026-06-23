using HVAC.EnergyMonitor.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Report;

public interface IEnergyReportService
{
    Task<IEnumerable<EnergyReportDto>> GetHourlyReportAsync(int pointId, DateTime start, DateTime end);
    Task<IEnumerable<EnergyReportDto>> GetDailyReportAsync(int pointId, DateTime start, DateTime end);
    Task<IEnumerable<EnergyReportDto>> GetMonthlyReportAsync(int pointId, DateTime start, DateTime end);
}
