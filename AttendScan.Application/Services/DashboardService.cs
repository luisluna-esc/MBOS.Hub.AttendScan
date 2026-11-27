using System.Net;
using System.Text.RegularExpressions;
using AttendScan.Application.Dtos.Dashboard;
using AttendScan.Application.Interfaces.Dashboard;
using AttendScan.core.Entities.Events;
using AttendScan.core.Entities.People;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResponseGetObject> GetSummary(long? eventSessionId)
    {
        // Son unos cientos de personas: se traen solo las columnas que se cuentan y se agrupa en memoria.
        var persons = await _unitOfWork.Repository<Person>().Query()
            .Select(p => new { p.PersonId, p.Gender, p.DelegateType, p.Region })
            .ToListAsync();

        HashSet<long>? attended = null;
        if (eventSessionId.HasValue)
        {
            attended = (await _unitOfWork.Repository<Attendance>().Query()
                .Where(a => a.EventSessionId == eventSessionId.Value)
                .Select(a => a.PersonId)
                .ToListAsync()).ToHashSet();
        }

        List<DashboardGroupDto> Group(Func<(long Id, string? Gender, string? Type, string? Region), string> key) =>
            persons
                .Select(p => (Id: p.PersonId, p.Gender, Type: p.DelegateType, p.Region))
                .GroupBy(p => key(p))
                .Select(g => new DashboardGroupDto
                {
                    Key = g.Key,
                    Total = g.Count(),
                    Attended = attended is null ? null : g.Count(p => attended.Contains(p.Id))
                })
                .ToList();

        var byGender = Group(p => p.Gender ?? string.Empty)
            .OrderBy(g => g.Key switch { "M" => 0, "F" => 1, _ => 2 })
            .ToList();

        var byType = Group(p => p.Type ?? string.Empty)
            .OrderByDescending(g => g.Total)
            .ThenBy(g => g.Key)
            .ToList();

        // "REGIÓN 2" antes que "REGIÓN 10"; sin región al final.
        var byRegion = Group(p => p.Region ?? string.Empty)
            .OrderBy(g => g.Key == string.Empty)
            .ThenBy(g => RegionNumber(g.Key))
            .ThenBy(g => g.Key)
            .ToList();

        return new ResponseGetObject
        {
            Data = new DashboardDto
            {
                TotalPersons = persons.Count,
                EventSessionId = eventSessionId,
                Attended = attended?.Count(id => persons.Any(p => p.PersonId == id)),
                ByGender = byGender,
                ByDelegateType = byType,
                ByRegion = byRegion
            },
            Messages = [],
            StatusCode = HttpStatusCode.OK
        };
    }

    private static int RegionNumber(string region)
    {
        var match = Regex.Match(region, @"\d+");
        return match.Success ? int.Parse(match.Value) : int.MaxValue;
    }
}
