using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/attendances")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class AttendancesController : ControllerBase
    {
		private readonly IAttendancesService _attendancesService;

		public AttendancesController(IAttendancesService attendancesService)
		{
			this._attendancesService = attendancesService;
		}

		[HttpGet("course_stats/{courseId}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<CourseAttendancesDto>>> GetCourseStats(Guid courseId)
		{
			var response = await _attendancesService.GetCourseAttendancesStatsAsync(courseId);
			return StatusCode(response.StatusCode, response);
		}

		[HttpGet("course_students/{courseId}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<List<CourseAttendancesStudentDto>>>> GetStudentsPagination(Guid courseId, bool? isActive = null, string searchTerm = "", int page = 1)
		{
			var response = await _attendancesService.GetStudentsAttendancesPaginationAsync(courseId, isActive, searchTerm, page);
			return StatusCode(response.StatusCode, response);
		}

		[HttpGet("student_stats")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<StudentAttendancesDto>>> GetStudentStats(StudentIdCourseIdDto dto)
		{
			var response = await _attendancesService.GetStudentAttendancesStatsAsync(dto);
			return StatusCode(response.StatusCode, response);
		}

		[HttpGet("student_attendances")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<List<AttendanceDto>>>> GetAttendancesByStudentPagination(StudentIdCourseIdDto dto, string searchTerm = "", int page = 1)
		{
			var response = await _attendancesService.GetAttendancesByStudentPaginationAsync(dto, searchTerm, page);
			return StatusCode(response.StatusCode, response);
		}
	}
}
