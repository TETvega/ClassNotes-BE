using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Attendances.Student;
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
		public async Task<ActionResult<ResponseDto<List<CourseAttendancesStudentDto>>>> GetStudentsPagination(Guid courseId, bool? isActive = null, string searchTerm = "", int page = 1,int? pageSize=null)
		{
			var response = await _attendancesService.GetStudentsAttendancesPaginationAsync(courseId, isActive, searchTerm, page, pageSize);
			return StatusCode(response.StatusCode, response);
		}

        [HttpPost("student_stats")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<StudentAttendancesDto>>> GetStudentStats(StudentIdCourseIdDto dto, bool isCurrentMonth = false)
        {
            var response = await _attendancesService.GetStudentAttendancesStatsAsync(dto, isCurrentMonth);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("student_attendances")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<AttendanceDto>>>> GetAttendancesByStudentPagination(StudentIdCourseIdDto dto, string searchTerm = "", int page = 1, bool isCurrentMonth = false, int pageSize = 10)
        {
            var response = await _attendancesService.GetAttendancesByStudentPaginationAsync(dto, searchTerm, page, isCurrentMonth, pageSize);
            return StatusCode(response.StatusCode, response);
        }

    }
}
