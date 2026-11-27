using AttendScan.Application.Dtos.Events;
using AttendScan.Application.Interfaces.Events;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendScan.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventSessionsController : ControllerBase
{
    private readonly IEventSessionService _eventSessionService;

    public EventSessionsController(IEventSessionService eventSessionService)
    {
        _eventSessionService = eventSessionService;
    }

    [Authorize(Policy = "eventSession.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryFilter paginationQueryFilter, [FromQuery] EventSessionQueryFilter queryFilter)
    {
        var result = await _eventSessionService.GetAllEventSessions(paginationQueryFilter, queryFilter);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "eventSession.manage")]
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] EventSessionDto dto)
    {
        var result = await _eventSessionService.InsertEventSession(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "eventSession.manage")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] EventSessionDto dto)
    {
        var result = await _eventSessionService.UpdateEventSession(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "eventSession.manage")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _eventSessionService.DeleteEventSession(id);
        return StatusCode((int)result.StatusCode, result);
    }
}
