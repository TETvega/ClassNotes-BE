using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Dashboard;
using ClassNotes.API.Services.DashboardHome;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers;

[Route("api/dashboard_home")]
[ApiController]
public class DashboardHomeController : ControllerBase
{
    private readonly IDashboardHomeService _dashboardHomeService;

    public DashboardHomeController(IDashboardHomeService dashboardHomeService)
    {
        this._dashboardHomeService = dashboardHomeService;
    }

    [HttpGet("info")]
    [Authorize(Roles = $"{RolesConstant.USER}")]
    public async Task<ActionResult<ResponseDto<DashboardHomeDto>>> GetDashboardInfo()
    {
        var result = await _dashboardHomeService.GetDashboardHomeAsync();
        return StatusCode(result.StatusCode, result);
    }
}
