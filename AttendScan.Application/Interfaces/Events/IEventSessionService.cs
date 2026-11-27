using AttendScan.Application.Dtos.Events;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;

namespace AttendScan.Application.Interfaces.Events;

public interface IEventSessionService
{
    Task<ResponseGetObject> GetAllEventSessions(PaginationQueryFilter paginationQueryFilter, EventSessionQueryFilter eventSessionQueryFilter);
    Task<ResponsePost> InsertEventSession(EventSessionDto eventSessionDto);
    Task<ResponsePost> UpdateEventSession(long id, EventSessionDto eventSessionDto);
    Task<ResponsePost> DeleteEventSession(long id);
}
