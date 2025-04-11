using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Students;
using iText.Kernel.Geom;
using MailKit.Search;
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

        [HttpGet("pendings/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<PendingClassesDto>>>> GetPendingActivitiesClases(Guid id, int? top = null  )
        {
            var response = await _studentsService.GetPendingActivitiesClasesListAsync(id , top);
            return StatusCode(response.StatusCode, new
            {
                response.Status,
                response.Message,
                response.Data,
            });
        }


        [HttpGet("pendingsList/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<StudentPendingDto>>>>> GetAllStudentsPendingActivitiesAsync(Guid id,string searchTerm = "",int? pageSize = null,int page = 1, string StudentType = "ALL", string ActivityType = "ALL")
        {
            var response = await _studentsService.GetAllStudentsPendingActivitiesAsync(id, searchTerm, pageSize, page, StudentType, ActivityType);
            return StatusCode(response.StatusCode, new
            {
                response.Status,
                response.Message,
                response.Data,
            });

        }

        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]
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
        [HttpPost("bulk-create")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<StudentResultDto>>> CreateBulkStudents(BulkStudentCreateDto bulkStudentCreateDto)
        {
            var response = await _studentsService.CreateStudentAsync(bulkStudentCreateDto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseDto<StudentDto>>> UpdateStudent(Guid id, StudentEditDto studentEditDto)
        {
            var response = await _studentsService.UpdateStudentAsync(id, studentEditDto);
            return StatusCode(response.StatusCode, response);
        }

        //EG -> Controlador de elimar estudiantes por arreglo o individual
        [HttpDelete("batch/{courseId}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<Guid>>>> DeleteStudentsInBatch(
       [FromBody] List<Guid> studentIds,
       [FromRoute] Guid courseId)
        {
            var response = await _studentsService.DeleteStudentsInBatchAsync(studentIds, courseId);
            return StatusCode(response.StatusCode, response);
        }

    }
}
