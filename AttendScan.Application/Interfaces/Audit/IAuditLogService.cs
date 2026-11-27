using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Audit;
using AttendScan.core.QueryFilters.Pagination;

namespace AttendScan.Application.Interfaces.Audit;

public interface IAuditLogService
{
    Task<ResponseGetObject> GetAllAuditLogs(PaginationQueryFilter paginationQueryFilter, AuditLogQueryFilter auditLogQueryFilter);
}