using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/students")]
	public class StudentsController : ControllerBase
	{
		private readonly IStudentsService _studentsService;

		public StudentsController(IStudentsService studentsService)
        {
			this._studentsService = studentsService;
		}

        [HttpGet]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<StudentDto>>>>> PaginationList(string searchTerm, int page = 1)
        {
            var response = await _studentsService.GetStudentsListAsync(searchTerm, page);
            return StatusCode(response.StatusCode, new
            {
                response.Status,
                response.Message,
                response.Data,
            });
        }
        [HttpPost]
        public async Task<ActionResult<ResponseDto<StudentDto>>> Create(StudentCreateDto studentCreateDto)
        {
            var response = await _studentsService.CreateStudentAsync(studentCreateDto);
            return StatusCode(response.StatusCode, response);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseDto<StudentDto>>> UpdateStudent(Guid id, StudentEditDto studentEditDto)
        {
            var response = await _studentsService.UpdateStudentAsync(id, studentEditDto);
            // Retornar una respuesta exitosa
            return StatusCode(response.StatusCode, response);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<ResponseDto<StudentDto>>> Delete(Guid id)
        {
            var response = await _studentsService.DeleteStudentAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
