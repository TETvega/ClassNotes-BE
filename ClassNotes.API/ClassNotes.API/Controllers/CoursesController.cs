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

        // Traer un curso mediante su nombre
        [HttpGet("{name}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CourseDto>>> Get(string name)
        {
            var response = await _coursesService.GetCourseByNameAsync(name);
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