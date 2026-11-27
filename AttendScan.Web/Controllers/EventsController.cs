using AttendScan.Application.Dtos.Events;
using AttendScan.Application.Interfaces.Events;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendScan.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [Authorize(Policy = "event.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryFilter paginationQueryFilter, [FromQuery] EventQueryFilter queryFilter)
    {
        var result = await _eventService.GetAllEvents(paginationQueryFilter, queryFilter);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "event.manage")]
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] EventDto dto)
    {
        var result = await _eventService.InsertEvent(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "event.manage")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] EventDto dto)
    {
        var result = await _eventService.UpdateEvent(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "event.manage")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _eventService.DeleteEvent(id);
        return StatusCode((int)result.StatusCode, result);
    }
}
