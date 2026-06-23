using HVAC.EnergyMonitor.Infrastructure.Repository;
using HVAC.EnergyMonitor.Models.DTOs;
using HVAC.EnergyMonitor.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HVAC.EnergyMonitor.Services.Report;

public class EnergyReportService : IEnergyReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public EnergyReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EnergyReportDto>> GetHourlyReportAsync(int pointId, DateTime start, DateTime end)
    {
        var values = await GetValuesAsync(pointId, start, end);
        return GroupByPeriod(values, start, end, TimeSpan.FromHours(1), "Hour");
    }

    public async Task<IEnumerable<EnergyReportDto>> GetDailyReportAsync(int pointId, DateTime start, DateTime end)
    {
        var values = await GetValuesAsync(pointId, start, end);
        return GroupByPeriod(values, start, end, TimeSpan.FromDays(1), "Day");
    }

    public async Task<IEnumerable<EnergyReportDto>> GetMonthlyReportAsync(int pointId, DateTime start, DateTime end)
    {
        var values = await GetValuesAsync(pointId, start, end);
        return GroupByMonth(values, start, end);
    }

    private async Task<List<PointValue>> GetValuesAsync(int pointId, DateTime start, DateTime end)
    {
        var repo = _unitOfWork.Repository<PointValue>();
        return (await repo.FindAsync(v => v.PointId == pointId && v.Timestamp >= start && v.Timestamp <= end))
            .OrderBy(v => v.Timestamp)
            .ToList();
    }

    private static IEnumerable<EnergyReportDto> GroupByPeriod(List<PointValue> values, DateTime start, DateTime end, TimeSpan period, string periodType)
    {
        var results = new List<EnergyReportDto>();
        var current = start;
        while (current < end)
        {
            var next = current + period;
            var periodValues = values.Where(v => v.Timestamp >= current && v.Timestamp < next).ToList();
            double total = periodValues.Any() ? periodValues.Average(v => v.Value) * periodValues.Count : 0;

            results.Add(new EnergyReportDto
            {
                PeriodStart = current,
                PeriodEnd = next,
                PeriodType = periodType,
                TotalValue = total,
                Unit = periodValues.FirstOrDefault()?.Point?.Unit ?? string.Empty
            });
            current = next;
        }
        return results;
    }

    private static IEnumerable<EnergyReportDto> GroupByMonth(List<PointValue> values, DateTime start, DateTime end)
    {
        var results = new List<EnergyReportDto>();
        var current = new DateTime(start.Year, start.Month, 1);
        while (current < end)
        {
            var next = current.AddMonths(1);
            var periodValues = values.Where(v => v.Timestamp >= current && v.Timestamp < next).ToList();
            double total = periodValues.Any() ? periodValues.Average(v => v.Value) * periodValues.Count : 0;

            results.Add(new EnergyReportDto
            {
                PeriodStart = current,
                PeriodEnd = next,
                PeriodType = "Month",
                TotalValue = total,
                Unit = periodValues.FirstOrDefault()?.Point?.Unit ?? string.Empty
            });
            current = next;
        }
        return results;
    }
}
