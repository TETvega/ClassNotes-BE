using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseSettings;
using ClassNotes.API.Services.CoursesSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/courses_settings")]
    public class CoursesSettingsController : ControllerBase
    {
        private readonly ICourseSettingsService _courseSettingsService;
        public CoursesSettingsController(ICourseSettingsService coursesSettingsService)
        {
            _courseSettingsService = coursesSettingsService;
        }

        //Traer listado de configuraciones
        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]

        public async Task<ActionResult<ResponseDto<CourseSettingDto>>> GetAll(
            string searchTerm = "",
            int page = 1
            )
        {
            var response = await _courseSettingsService.GetCourseSettingsListAsync(searchTerm, page);
            return StatusCode(response.StatusCode, response);
        }

        // Traer una configuración mediante su id
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseSettingDto>>> Get(Guid id)
        {
            var response = await _courseSettingsService.GetCourseSettingByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        // Crear una configuración
        [HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseSettingDto>>> Create(CourseSettingCreateDto dto)
        {
            var response = await _courseSettingsService.CreateAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        //Editar configuración 
        [HttpPut("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseSettingDto>>> Edit(CourseSettingEditDto dto, Guid id)
        {
            var response = await _courseSettingsService.EditAsync(dto, id);
            return StatusCode(response.StatusCode, response);
        }


        // Eliminar una configuración
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseSettingDto>>> Delete(Guid id)
        {
            var response = await _courseSettingsService.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}