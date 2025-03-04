using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.QR;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Services.Distance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class QRController : ControllerBase
{
    private readonly ClassNotesContext _context;
    private readonly DistanceService _distanceService;
    private readonly IAttendancesService _attendanceService;

    public QRController(
        ClassNotesContext context,
        DistanceService distanceService,
        IAttendancesService attendanceService)
    {
        _context = context;
        _distanceService = distanceService;
        _attendanceService = attendanceService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateQR([FromBody] QRValidationRequestDto request)
    {
        var qrParts = request.QRContent.Split('|');
        if (qrParts.Length < 6) return BadRequest("Código QR inválido.");

        string profesorId = qrParts[0];
        Guid centroId = Guid.Parse(qrParts[1]);
        Guid claseId = Guid.Parse(qrParts[2]);
        double latitud = double.Parse(qrParts[3]);
        double longitud = double.Parse(qrParts[4]);
        DateTime qrTimestamp = DateTime.Parse(qrParts[5]);

        // Validar si el QR ha expirado (5 minutos)
        if (DateTime.UtcNow > qrTimestamp.AddMinutes(5))
        {
            return BadRequest("El código QR ha expirado.");
        }

        // Validar si el estudiante está dentro del rango de 15 metros usando el servicio
        double distancia = _distanceService.CalcularDistancia(latitud, longitud, request.EstudianteLatitud, request.EstudianteLongitud);
        if (distancia > 15)
        {
            return BadRequest("Estás fuera del rango permitido para confirmar la asistencia.");
        }

        // Buscar al estudiante por su correo electrónico
        var estudiante = await _context.Students
            .FirstOrDefaultAsync(s => s.Email == request.EstudianteCorreo);
        if (estudiante == null)
        {
            return BadRequest("El estudiante no está registrado.");
        }

        // Crear el DTO para la asistencia
        var attendanceCreateDto = new AttendanceCreateDto
        {
            Attended = "Presente", // O cualquier otro valor que indique asistencia
            CourseId = claseId,
            StudentId = estudiante.Id
        };

        // Crear la asistencia utilizando el servicio
        try
        {
            var attendanceDto = await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);
            return Ok(new
            {
                Message = "Asistencia confirmada y registrada.",
                Estudiante = request.EstudianteNombre,
                Correo = request.EstudianteCorreo,
                Attendance = attendanceDto
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}