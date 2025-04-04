using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Allcourses;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseFilter;
using ClassNotes.API.Services.AllCourses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [Route("api/courses-filter")]
    [ApiController]
    public class CoursesFilterController : ControllerBase
    {
        private readonly ICoursesFilterService _filterService;

        public CoursesFilterController(ICoursesFilterService filterService)
        {
            _filterService = filterService;
        }

        [HttpPost("all")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<CourseCenterDto>>>>> GetAllCourses([FromBody] CoursesFilterDto filter)
        {
            var response = await _filterService.GetFilteredCourses(filter);
            return StatusCode(response.StatusCode, response);
        }
    }
}
