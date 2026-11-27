using AttendScan.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendScan.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Resumen para el Inicio. Con eventSessionId incluye la asistencia de esa sesión.</summary>
    [Authorize(Policy = "dashboard.view")]
    [HttpGet]
    public async Task<IActionResult> GetSummary([FromQuery] long? eventSessionId)
    {
        var result = await _dashboardService.GetSummary(eventSessionId);
        return StatusCode((int)result.StatusCode, result);
    }
}
