using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.AttendacesRealTime.ForStudents;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.AttendanceRealTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/attendancesR")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AttendaceRealTimeController : ControllerBase
    {
        private readonly IAttendanceRSignalService _attendanceRSignalService;

        public AttendaceRealTimeController(
            IAttendanceRSignalService attendanceRSignalService
            )
        {
            _attendanceRSignalService = attendanceRSignalService;
        }

        [HttpPost("send_attendanceR")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<object>>> SendAttendance(AttendanceRequestDto request)
        {
            var result = await _attendanceRSignalService.ProcessAttendanceAsync(request);

            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("create_my_attendance_OTP")]
        public async Task<ActionResult<ResponseDto<StudentAttendanceResponse>>>  CreateAttendanceOTP(string email, string OTP, float x, float y , Guid courseId)
        {
          var result = await  _attendanceRSignalService.SendAttendanceByOtpAsync(email,OTP,x,y,courseId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
