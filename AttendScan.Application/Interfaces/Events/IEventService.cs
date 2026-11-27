using AttendScan.Application.Dtos.Events;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;

namespace AttendScan.Application.Interfaces.Events;

public interface IEventService
{
    Task<ResponseGetObject> GetAllEvents(PaginationQueryFilter paginationQueryFilter, EventQueryFilter eventQueryFilter);
    Task<ResponsePost> InsertEvent(EventDto eventDto);
    Task<ResponsePost> UpdateEvent(long id, EventDto eventDto);
    Task<ResponsePost> DeleteEvent(long id);
}
