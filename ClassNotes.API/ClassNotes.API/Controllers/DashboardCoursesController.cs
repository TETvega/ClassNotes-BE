using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboardCourses;
using ClassNotes.API.Services.DashboardCourses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    // --------------------- CP --------------------- //
    [ApiController]
    [Route("api/dashboard_courses")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class DashboardCoursesController : ControllerBase
    {
        private readonly IDashboardCoursesService _dashboardCoursesService;

        public DashboardCoursesController(IDashboardCoursesService dashboardCoursesService)
        {
            _dashboardCoursesService = dashboardCoursesService;
        }

        [HttpGet("{courseId}/info")] // En el bruno se debe de poner el id del curso y luego el /info
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<DashboardCourseDto>>> GetDashboardInfo(Guid courseId)
        {
            var result = await _dashboardCoursesService.GetDashboardCourseAsync(courseId);
            return StatusCode(result.StatusCode, result);
        }
    }
}