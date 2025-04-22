using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Notes;
using ClassNotes.API.Dtos.Notes.QualificationDasboard;
using ClassNotes.API.Services.Notes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/notes")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class NotesController : ControllerBase
    {
		private readonly INotesService _notesService;

		public NotesController(INotesService notesService)
        {
			_notesService = notesService;
		}



        [HttpGet("units")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<CenterDto>>>> GetAllStudentUnitNotes(Guid studentId, Guid courseId, int page = 1)
        {
            var response = await _notesService.GetStudentUnitsNotesAsync(studentId, courseId, page);
            return StatusCode(response.StatusCode, response);
        }


        [HttpGet("student_notes")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<CenterDto>>>> GetAllStudentNotes(Guid courseId, int page = 1)
        {
            var response = await _notesService.GetStudentsNotesAsync( courseId, page);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("student_activities")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<CenterDto>>>> GetAllStudentActivities(Guid courseId, int page = 1)
        {
            var response = await _notesService.GetStudentsActivitiesAsync(courseId, page);
            return StatusCode(response.StatusCode, response);
        }


        [HttpGet("student/dashboard/{courseId}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        /// <summary>
        /// Este endpoint retorna las estadísticas generales del dashboard de calificaciones,
        /// junto con la lista paginada de calificaciones por estudiante.
        /// </summary>
        /// <param name="courseId">ID del curso</param>
        /// <param name="activeStudent">[ALL | ACTIVE | INACTIVE] - Filtro por estado del estudiante</param>
        /// <param name="studentStateNote">[ALL | EXCELLENT | GOOD | LOW | FAILED] - Filtro por estado de nota</param>
        /// <param name="includeStats">Si se deben incluir las estadísticas generales (solo en la primera carga)</param>
        /// <param name="page">Página actual para paginación</param>
        /// <param name="pageSize">Cantidad de elementos por página</param>
        public async Task<ActionResult<ResponseDto<DasboardRequestDto>>> GetAllStudentNotesUnits(
            Guid courseId,
            string activeStudent = null,
            string studentStateNote = null,
            bool includeStats = true,
            int page = 1,
            int pageSize = 10,
            string searchTerm = ""
        )
        {
            var response = await _notesService.GetDashboardQualifications(
                courseId,
                activeStudent,
                studentStateNote,
                includeStats,
                page,
                pageSize,
                searchTerm
            );

            return StatusCode(response.StatusCode, response);
        }
    }
}
