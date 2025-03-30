using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services;
using Microsoft.AspNetCore.Authorization;
using ClassNotes.API.Dtos.Common;
using Azure;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class EmailAttendanceController : ControllerBase
    {
        private readonly IEmailAttendanceService _emailAttendanceService;

        public EmailAttendanceController(IEmailAttendanceService emailAttendanceService)
        {
            _emailAttendanceService = emailAttendanceService;
        }

        [HttpPost("send-emails")]
        public async Task<ActionResult<ResponseDto<SendEmailsStatusDto>>> SendEmails( EmailAttendanceRequestDto request)
        {
                  var response =  await _emailAttendanceService.SendEmailsAsync(request);
                 return StatusCode(response.StatusCode, response);
        }

        [HttpPost("validate-attendance")]
        public async Task<IActionResult> ValidateAttendance([FromBody] ValidateAttendanceRequestDto request)
        {
            try
            {
                await _emailAttendanceService.ValidateAttendanceAsync(request);
                return Ok(new { Message = "Asistencia validada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}