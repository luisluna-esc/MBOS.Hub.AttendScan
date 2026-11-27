using AttendScan.Application.Dtos.Events;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;

namespace AttendScan.Application.Interfaces.Events;

public interface IAttendanceService
{
    Task<ResponseGetObject> GetAllAttendances(PaginationQueryFilter paginationQueryFilter, AttendanceQueryFilter attendanceQueryFilter);
    Task<ResponseGetObject> Scan(ScanRequestDto scanRequestDto);
}
