using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Services.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CoursesController : ControllerBase
    {
        private readonly ICoursesService _coursesService;
        public CoursesController(ICoursesService coursesService)
        {
            _coursesService = coursesService;
        }

        //Traer listado de cursos 
        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]

        public async Task<ActionResult<ResponseDto<CourseDto>>> GetAll(
            string searchTerm = "",
            int page = 1
            )
            {
            var response = await _coursesService.GetCoursesListAsync(searchTerm, page);
            return StatusCode(response.StatusCode, response);
            }

        // Traer un curso mediante su id
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseDto>>> Get(Guid id)
        {
            var response = await _coursesService.GetCourseByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        // Crear un curso
        [HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseDto>>> Create(CourseCreateDto dto)
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


        // Eliminar un curso
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseDto>>> Delete(Guid id)
        {
            var response = await _coursesService.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}