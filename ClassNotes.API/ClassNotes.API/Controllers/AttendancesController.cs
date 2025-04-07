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
		public async Task<ActionResult<ResponseDto<List<CourseAttendancesStudentDto>>>> GetStudentsPagination(Guid courseId, bool? isActive = null, string searchTerm = "", int page = 1)
		{
			var response = await _attendancesService.GetStudentsAttendancesPaginationAsync(courseId, isActive, searchTerm, page);
			return StatusCode(response.StatusCode, response);
		}

        [HttpGet("student_stats")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<StudentAttendancesDto>>> GetStudentStats(StudentIdCourseIdDto dto, bool isCurrentMonth = false)
        {
            var response = await _attendancesService.GetStudentAttendancesStatsAsync(dto, isCurrentMonth);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("student_attendances")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<AttendanceDto>>>> GetAttendancesByStudentPagination(StudentIdCourseIdDto dto, string searchTerm = "", int page = 1, bool isCurrentMonth = false, int pageSize = 10)
        {
            var response = await _attendancesService.GetAttendancesByStudentPaginationAsync(dto, searchTerm, page, isCurrentMonth, pageSize);
            return StatusCode(response.StatusCode, response);
        }


        /// <summary>
        /// MOVER TODA LA LOGICA A LOS SERVICIOS DIRECTAMENTE 
        /// </summary>
        /// <param name="attendanceCreateDto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<AttendanceDto>> CreateAttendance([FromBody] AttendanceCreateDto attendanceCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var attendance = await _attendancesService.CreateAttendanceAsync(attendanceCreateDto);
                return CreatedAtAction(
                    nameof(GetAttendanceById),
                    new { id = attendance.Id },
                    attendance);
            }
            catch (ArgumentException ex)
            {
                //_logger.LogWarning(ex, "Argument error");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Server error");
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAllAttendances()
        {
            try
            {
                var attendances = await _attendancesService.ListAttendancesAsync();
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Server error");
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AttendanceDto>> GetAttendanceById(Guid id)
        {
            try
            {
                var attendance = (await _attendancesService.ListAttendancesAsync())
                    .FirstOrDefault(a => a.Id == id);

                return attendance != null ? Ok(attendance) : NotFound();
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Server error");
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Detail = ex.Message
                });
            }
        }
    }
}
