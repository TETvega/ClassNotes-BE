using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassNotes.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendancesService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            IAttendancesService attendanceService,
            ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<AttendanceDto>> CreateAttendance([FromBody] AttendanceCreateDto attendanceCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var attendance = await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);
                return CreatedAtAction(
                    nameof(GetAttendanceById),
                    new { id = attendance.Id },
                    attendance);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error");
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
                var attendances = await _attendanceService.ListAttendancesAsync();
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error");
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
                var attendance = (await _attendanceService.ListAttendancesAsync())
                    .FirstOrDefault(a => a.Id == id);

                return attendance != null ? Ok(attendance) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error");
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Detail = ex.Message
                });
            }
        }
    }
}