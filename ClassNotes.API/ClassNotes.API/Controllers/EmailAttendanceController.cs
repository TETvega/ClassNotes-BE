using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos;
using ClassNotes.API.Services;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Services.Emails;
using Microsoft.Extensions.Logging;
using ClassNotes.API.Services.Distance;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailAttendanceController : ControllerBase
    {
        private readonly ClassNotesContext _context;
        private readonly IEmailsService _emailsService;
        private readonly ILogger<EmailAttendanceController> _logger;
        private readonly DistanceService _distanceService;
        private readonly IAttendancesService _attendanceService;

        // Lista estática para almacenar los OTPs
        public static List<StudentOTPDto> OTPList = new List<StudentOTPDto>();

        public EmailAttendanceController(
            ClassNotesContext context,
            IEmailsService emailsService,
            ILogger<EmailAttendanceController> logger,
            DistanceService distanceService,
            IAttendancesService attendanceService)
        {
            _context = context;
            _emailsService = emailsService;
            _logger = logger;
            _distanceService = distanceService;
            _attendanceService = attendanceService;
        }

        [HttpPost("send-emails")]
        public async Task<IActionResult> SendEmails([FromBody] EmailAttendanceRequestDto request)
        {
            try
            {
                // Validar el rango de validación
                if (request.RangoValidacionMetros < 15 || request.RangoValidacionMetros > 100)
                {
                    return BadRequest("El rango de validación debe estar entre 15 y 100 metros.");
                }

                // Validar el tiempo de expiración del OTP
                if (request.TiempoExpiracionOTPMinutos < 3)
                {
                    return BadRequest("El tiempo de expiración del OTP debe ser de al menos 3 minutos.");
                }

                // Obtener la clase con los estudiantes asociados
                var clase = await _context.Courses
                    .Include(c => c.Students)
                        .ThenInclude(sc => sc.Student)
                    .Where(c => c.Id == request.ClaseId && c.TeacherId == request.ProfesorId && c.CenterId == request.CentroId)
                    .FirstOrDefaultAsync();

                if (clase == null)
                {
                    return BadRequest("La clase no está asociada con el profesor o el centro.");
                }

                // Obtener la lista de estudiantes asociados a la clase
                var estudiantes = clase.Students
                    .Select(sc => sc.Student)
                    .ToList();

                if (!estudiantes.Any())
                {
                    return BadRequest("No hay estudiantes asociados a esta clase.");
                }

                // Lista para almacenar los destinatarios y los correos enviados
                var destinatarios = new List<object>();

                // Enviar correos a los estudiantes
                foreach (var estudiante in estudiantes)
                {
                    if (string.IsNullOrEmpty(estudiante.Email))
                    {
                        _logger.LogWarning($"El estudiante {estudiante.FirstName} no tiene un correo electrónico válido.");
                        continue;
                    }

                    // Generar un OTP único
                    var otp = GenerateOTP();

                    // Crear el DTO para el OTP
                    var studentOTP = new StudentOTPDto
                    {
                        OTP = otp,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        StudentId = estudiante.Id,
                        CourseId = clase.Id,
                        ExpirationDate = DateTime.UtcNow.AddMinutes(request.TiempoExpiracionOTPMinutos), // Usar el tiempo configurado por el maestro
                        RangoValidacionMetros = request.RangoValidacionMetros // Incluir el rango de validación
                    };

                    // Almacenar el OTP en la lista estática
                    EmailAttendanceController.OTPList.Add(studentOTP);

                    // Crear el objeto EmailDto con el enlace de validación y el OTP
                    var emailDto = new EmailDto
                    {
                        To = estudiante.Email,
                        Subject = "Validación de Asistencia",
                        Content = $"Hola {estudiante.FirstName}, para validar tu asistencia a la clase '{clase.Name}', utiliza el siguiente código OTP: {otp}. " +
                                  $"También puedes hacer clic en el siguiente enlace para validar tu asistencia: " +
                                  $"https://tudominio.com/validate-attendance?otp={otp}"
                    };

                    // Enviar el correo
                    var result = await _emailsService.SendEmailAsync(emailDto);

                    if (!result.Status)
                    {
                        _logger.LogError($"Error al enviar el correo a {estudiante.Email}: {result.Message}");
                    }
                    else
                    {
                        // Agregar el destinatario y el correo a la lista
                        destinatarios.Add(new
                        {
                            Nombre = estudiante.FirstName,
                            Correo = estudiante.Email
                        });
                    }
                }

                // Retornar la lista de destinatarios y un mensaje de éxito
                return Ok(new
                {
                    Message = "Correos enviados correctamente.",
                    Destinatarios = destinatarios,
                    RangoValidacionMetros = request.RangoValidacionMetros,
                    TiempoExpiracionOTPMinutos = request.TiempoExpiracionOTPMinutos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correos electrónicos");
                return StatusCode(500, new { Message = "Ocurrió un error inesperado al enviar los correos." });
            }
        }

        [HttpPost("validate-attendance")]
        public async Task<IActionResult> ValidateAttendance([FromBody] ValidateAttendanceRequestDto request)
        {
            try
            {
                // Buscar el OTP en la lista estática
                var studentOTP = OTPList.FirstOrDefault(sotp => sotp.OTP == request.OTP);

                if (studentOTP == null || studentOTP.ExpirationDate < DateTime.UtcNow)
                {
                    return BadRequest("OTP inválido o expirado.");
                }

                // Calcular la distancia entre la ubicación proporcionada y la almacenada
                var distance = _distanceService.CalcularDistancia(
                    request.Latitude, request.Longitude,
                    studentOTP.Latitude, studentOTP.Longitude);

                // Validar si el estudiante está dentro del rango permitido
                if (distance > studentOTP.RangoValidacionMetros)
                {
                    return BadRequest($"Debes estar dentro de un radio de {studentOTP.RangoValidacionMetros} metros para validar tu asistencia.");
                }

                // Obtener el estudiante por su ID
                var estudiante = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == studentOTP.StudentId);

                if (estudiante == null)
                {
                    return BadRequest("Estudiante no encontrado.");
                }

                // Crear el DTO para la asistencia
                var attendanceCreateDto = new AttendanceCreateDto
                {
                    Attended = "Presente", // Marcar como "Presente"
                    CourseId = studentOTP.CourseId, // Tomar el CourseId del OTP
                    StudentId = studentOTP.StudentId
                };

                // Crear la asistencia utilizando el servicio
                var attendanceDto = await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);

                // Eliminar el OTP de la lista estática después de usarlo
                OTPList.Remove(studentOTP);

                // Retornar la respuesta con los datos del estudiante y la asistencia
                return Ok(new
                {
                    Message = "Asistencia validada correctamente.",
                    Estudiante = new
                    {
                        Id = estudiante.Id,
                        Nombre = estudiante.FirstName,
                        Correo = estudiante.Email
                    },
                    Asistencia = new
                    {
                        Id = attendanceDto.Id,
                        Estado = attendanceDto.Attended,
                        Fecha = attendanceDto.RegistrationDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la asistencia");
                return StatusCode(500, new { Message = "Ocurrió un error inesperado al validar la asistencia." });
            }
        }

        private string GenerateOTP()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}