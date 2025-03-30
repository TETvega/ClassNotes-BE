using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<IActionResult> SendEmails([FromBody] EmailAttendanceRequestDto request)
        {
            try
            {
                await _emailAttendanceService.SendEmailsAsync(request);
                return Ok(new { Message = "Correos enviados correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
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