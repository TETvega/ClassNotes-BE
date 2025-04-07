using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.AttendanceRealTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/attendancesR")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AttendaceRealTimeController: ControllerBase
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

    }
}
