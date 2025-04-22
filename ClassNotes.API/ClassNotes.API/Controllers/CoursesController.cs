using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseFilter;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.DashboardCourses;
using ClassNotes.API.Services.AllCourses;
using ClassNotes.API.Services.Courses;
using ClassNotes.API.Services.DashboardCourses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CoursesController : ControllerBase
    {
        private readonly ICoursesFilterService _filterService;
        private readonly IDashboardCoursesService _dashboardCoursesService;
        private readonly ICoursesService _coursesService;
        public CoursesController(
            ICoursesFilterService filterService,
            IDashboardCoursesService dashboardCoursesService,
            ICoursesService coursesService)
        {
            _filterService = filterService;
            _dashboardCoursesService = dashboardCoursesService;
            _coursesService = coursesService;
        }

        [HttpPost("all")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<CourseCenterDto>>>>> GetAllCourses( CoursesFilterDto filter)
        {
            var response = await _filterService.GetFilteredCourses(filter);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{courseId}/info")] // En el bruno se debe de poner el id del curso y luego el /info
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<DashboardCourseDto>>> GetDashboardInfo(Guid courseId)
        {
            var result = await _dashboardCoursesService.GetDashboardCourseAsync(courseId);
            return StatusCode(result.StatusCode, result);
        }
        //Traer listado de cursos 
        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]

        public async Task<ActionResult<ResponseDto<CourseWithSettingDto>>> GetAll(
            string searchTerm = "",
            int page = 1,
            int? pageSize = null
            )
        {
            var response = await _coursesService.GetCoursesListAsync(searchTerm, page, pageSize);
            return StatusCode(response.StatusCode, response);
        }

        // Traer un curso mediante su id
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseWithSettingDto>>> Get(Guid id)
        {
            var response = await _coursesService.GetCourseByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        // Traer unidades de curso mediante su id
        [HttpGet("units/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<UnitDto>>>> GetUnits(Guid id)
        {
            var response = await _coursesService.GetCourseUnits(id);
            return StatusCode(response.StatusCode, response);
        }

        // Crear un curso
        [HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseWithSettingDto>>> Create(CourseWithSettingCreateDto dto)
        {
            var response = await _coursesService.CreateAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        //Editar curso 
        [HttpPut("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseDto>>> Edit(CourseEditDto dto, Guid id)
        {
            var response = await _coursesService.EditAsync(dto, id);
            return StatusCode(response.StatusCode, response);
        }


        //Editar curso 
        [HttpPut("ubication/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseDto>>> EditUbicationCourse(LocationDto dto, Guid id)
        {
            var response = await _coursesService.EditUbicationAsync(dto, id);
            return StatusCode(response.StatusCode, response);
        }

        // Eliminar un curso
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseWithSettingDto>>> Delete(Guid id)
        {
            var response = await _coursesService.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}