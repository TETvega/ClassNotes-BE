using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClassNotes.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendancesService _attendanceService;

        public AttendanceController(IAttendancesService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        //DD: Creacion de attendance 
        [HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<AttendanceDto>> CreateAttendance([FromBody] AttendanceCreateDto attendanceCreateDto)
        {
            try
            {
                var attendance = await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);
                return CreatedAtAction(nameof(GetAttendanceById), new { id = attendance.Id }, attendance);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error interno al crear la asistencia.");
            }
        }

        // DD: Edicion de Assistencia 
        [HttpPut("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<AttendanceDto>> EditAttendance(Guid id, [FromBody] AttendanceEditDto attendanceEditDto)
        {
            try
            {
                var attendance = await _attendanceService.EditAttendanceAsync(id, attendanceEditDto);
                return Ok(attendance);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error interno al editar la asistencia.");
            }
        }

        // dD: Listar Asistencia 
        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<List<AttendanceDto>>> GetAllAttendances()
        {
            try
            {
                var attendances = await _attendanceService.ListAttendancesAsync();
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error interno al obtener las asistencias.");
            }
        }

        // DD: Listar Asistencia por Curso Id 
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<List<AttendanceDto>>> GetAttendancesByCourse(Guid courseId)
        {
            try
            {
                var attendances = await _attendanceService.ListAttendancesByCourseAsync(courseId);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error interno al obtener las asistencias por curso.");
            }
        }

        //DD: Listar Asistencia por Estudiante Id
        [HttpGet("student/{studentId}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<List<AttendanceDto>>> GetAttendancesByStudent(Guid studentId)
        {
            try
            {
                var attendances = await _attendanceService.ListAttendancesByStudentAsync(studentId);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error interno al obtener las asistencias por estudiante.");
            }
        }

        //DD: Listar por Id 
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<AttendanceDto>> GetAttendanceById(Guid id)
        {
            try
            {
                var attendances = await _attendanceService.ListAttendancesAsync();
                var attendance = attendances.FirstOrDefault(a => a.Id == id);
                if (attendance == null)
                {
                    return NotFound("La asistencia no fue encontrada.");
                }
                return Ok(attendance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error interno al obtener la asistencia.");
            }
        }
    }
}