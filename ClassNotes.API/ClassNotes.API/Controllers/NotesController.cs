using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
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
    }
}
