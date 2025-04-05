using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboarCenter;
using ClassNotes.API.Dtos.Dashboard;
using ClassNotes.API.Services.DashboarCenter;
using ClassNotes.API.Services.DashboardHome;
using iText.Kernel.Geom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [Route("api/dashboard_center")]
    [ApiController]
    public class DashboarCenterController : Controller
    {
        private readonly IDashboardCenterService _dashboardCenterService;

        public DashboarCenterController(IDashboardCenterService dashboardHomeService)
        {
            this._dashboardCenterService = dashboardHomeService;
        }

        [HttpGet("info")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<DashboardCenterDto>>> GetDashboardInfo(Guid centerId, string searchTerm = "", int page = 1, int? pageSize =null , string classType= null)
        {
            var result = await _dashboardCenterService.GetDashboardCenterAsync(centerId, searchTerm, page, pageSize , classType);
            return StatusCode(result.StatusCode, result);
        }

    }
}
