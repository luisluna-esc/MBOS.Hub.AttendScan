using AttendScan.Application.Dtos.Events;
using AttendScan.Application.Interfaces.Events;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendScan.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendancesController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendancesController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [Authorize(Policy = "attendance.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryFilter paginationQueryFilter, [FromQuery] AttendanceQueryFilter attendanceQueryFilter)
    {
        var result = await _attendanceService.GetAllAttendances(paginationQueryFilter, attendanceQueryFilter);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "attendance.scan")]
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto scanRequestDto)
    {
        var result = await _attendanceService.Scan(scanRequestDto);
        return StatusCode((int)result.StatusCode, result);
    }
}
