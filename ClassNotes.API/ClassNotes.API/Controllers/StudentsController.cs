using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/students")]
	public class StudentsController : ControllerBase
	{
		private readonly IStudentsService _studentsService;

		public StudentsController(IStudentsService studentsService)
        {
			_studentsService = studentsService;
		}

        [HttpGet]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<StudentDto>>>>> PaginationList(string searchTerm, int? pageSize = null, int page = 1)
        {
            var response = await _studentsService.GetStudentsListAsync(searchTerm,pageSize, page);
            return StatusCode(response.StatusCode, new
            {
                response.Status,
                response.Message,
                response.Data,
            });

        }

        // EG -> Controlador de obtener estudiante por id
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<StudentDto>>> GetById(Guid id) 
        { 
            var response = await _studentsService.GetStudentByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        //EG -> Controlador de Create aplicando el modo estricto 
        [HttpPost]
        public async Task<ActionResult<ResponseDto<StudentDto>>> Create(
        [FromBody] StudentCreateDto studentCreateDto,
        [FromHeader(Name = "Strict-Mode")] bool strictMode = false)
        {
            var response = await _studentsService.CreateStudentAsync(studentCreateDto, strictMode);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseDto<StudentDto>>> UpdateStudent(Guid id, StudentEditDto studentEditDto)
        {
            var response = await _studentsService.UpdateStudentAsync(id, studentEditDto);
            return StatusCode(response.StatusCode, response);
        }
     
        //EG -> Controlador de elimar estudiantes por arreglo o individual 
        [HttpDelete("batch")]
        public async Task<ActionResult<ResponseDto<List<Guid>>>> DeleteStudentsInBatch([FromBody] List<Guid> studentIds)
        {
            var response = await _studentsService.DeleteStudentsInBatchAsync(studentIds);
            return StatusCode(response.StatusCode, response);
        }

    }
}
