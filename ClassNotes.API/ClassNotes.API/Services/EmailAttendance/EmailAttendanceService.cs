using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services;
using ClassNotes.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Services.Distance;
using ClassNotes.API.Services.Emails;

namespace ClassNotes.API.Services
{
    public class EmailAttendanceService : IEmailAttendanceService
    {
        private readonly ClassNotesContext _context;
        private readonly IEmailsService _emailsService;
        private readonly ILogger<EmailAttendanceService> _logger;
        private readonly DistanceService _distanceService;
        private readonly IAttendancesService _attendanceService;
        private readonly OTPCleanupService _otpCleanupService;
        private readonly IHubContext<AttendanceHub> _hubContext;
        private readonly EmailScheduleService _emailScheduleService;

        // Lista privada para almacenar los OTPs
        private static List<StudentOTPDto> _otpList = new List<StudentOTPDto>();

        public EmailAttendanceService(
            ClassNotesContext context,
            IEmailsService emailsService,
            ILogger<EmailAttendanceService> logger,
            DistanceService distanceService,
            IAttendancesService attendanceService,
            OTPCleanupService otpCleanupService,
            IHubContext<AttendanceHub> hubContext,
            EmailScheduleService emailScheduleService)
        {
            _context = context;
            _emailsService = emailsService;
            _logger = logger;
            _distanceService = distanceService;
            _attendanceService = attendanceService;
            _otpCleanupService = otpCleanupService;
            _hubContext = hubContext;
            _emailScheduleService = emailScheduleService;
        }

        public async Task SendEmailsAsync(EmailAttendanceRequestDto request)
        {
            try
            {
                // Validar el rango de validación
                if (request.RangoValidacionMetros < 15 || request.RangoValidacionMetros > 100)
                {
                    throw new ArgumentException("El rango de validación debe estar entre 15 y 100 metros.");
                }

                // Validar el tiempo de expiración del OTP (mínimo 1 minuto, máximo 30 minutos)
                if (request.TiempoExpiracionOTPMinutos < 1 || request.TiempoExpiracionOTPMinutos > 30)
                {
                    throw new ArgumentException("El tiempo de expiración del OTP debe estar entre 1 y 30 minutos.");
                }

                // Obtener la clase con los estudiantes asociados
                var clase = await _context.Courses
                    .Include(c => c.Students)
                        .ThenInclude(sc => sc.Student)
                    .Where(c => c.Id == request.ClaseId && c.TeacherId == request.ProfesorId && c.CenterId == request.CentroId)
                    .FirstOrDefaultAsync();

                if (clase == null)
                {
                    throw new ArgumentException("La clase no está asociada con el profesor o el centro.");
                }

                // Obtener la lista de estudiantes asociados a la clase
                var estudiantes = clase.Students
                    .Select(sc => sc.Student)
                    .ToList();

                if (!estudiantes.Any())
                {
                    throw new ArgumentException("No hay estudiantes asociados a esta clase.");
                }

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
                        ExpirationDate = DateTime.UtcNow.AddMinutes(request.TiempoExpiracionOTPMinutos),
                        RangoValidacionMetros = request.RangoValidacionMetros
                    };

                    // Almacenar el OTP en la lista privada
                    AddOTP(studentOTP);

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
                        // Notificar a los clientes en tiempo real
                        await _hubContext.Clients.All.SendAsync("ReceiveEmailSent", new
                        {
                            StudentName = estudiante.FirstName,
                            Email = estudiante.Email,
                            OTP = otp
                        });
                    }
                }

                // Enviar la lista de OTPs activos al servicio de limpieza
                SendActiveOTPsToCleanupService();

                // Guardar las preferencias para envío automático
                if (request.EnvioAutomatico)
                {
                    _emailScheduleService.AddOrUpdateConfig(
                        request.ClaseId, // Guid
                        true, // EnvioAutomatico
                        TimeSpan.Parse(request.HoraEnvio), // HoraEnvio
                        request.DiasEnvio.Select(d => (DayOfWeek)d).ToList() // DiasEnvio
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correos electrónicos");
                throw;
            }
        }

        public async Task ValidateAttendanceAsync(ValidateAttendanceRequestDto request)
        {
            try
            {
                // Buscar el OTP en la lista privada
                var studentOTP = _otpList.FirstOrDefault(sotp => sotp.OTP == request.OTP);

                if (studentOTP == null || studentOTP.ExpirationDate < DateTime.UtcNow)
                {
                    throw new ArgumentException("OTP inválido o expirado.");
                }

                // Calcular la distancia entre la ubicación proporcionada y la almacenada
                var distance = _distanceService.CalcularDistancia(
                    request.Latitude, request.Longitude,
                    studentOTP.Latitude, studentOTP.Longitude);

                // Validar si el estudiante está dentro del rango permitido
                if (distance > studentOTP.RangoValidacionMetros)
                {
                    throw new ArgumentException($"Debes estar dentro de un radio de {studentOTP.RangoValidacionMetros} metros para validar tu asistencia.");
                }

                // Obtener el estudiante por su ID
                var estudiante = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == studentOTP.StudentId);

                if (estudiante == null)
                {
                    throw new ArgumentException("Estudiante no encontrado.");
                }

                // Crear el DTO para la asistencia
                var attendanceCreateDto = new AttendanceCreateDto
                {
                    Attended = "Presente",
                    CourseId = studentOTP.CourseId,
                    StudentId = studentOTP.StudentId
                };

                // Crear la asistencia utilizando el servicio
                var attendanceDto = await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);

                // Eliminar el OTP de la lista privada después de usarlo
                RemoveOTP(studentOTP);

                // Notificar a los clientes en tiempo real
                await _hubContext.Clients.All.SendAsync("ReceiveAttendanceValidation", new
                {
                    StudentName = estudiante.FirstName,
                    Email = estudiante.Email,
                    AttendanceStatus = "Presente",
                    ValidationTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la asistencia");
                throw;
            }
        }

        // Método para agregar un OTP a la lista
        public void AddOTP(StudentOTPDto otp)
        {
            _otpList.Add(otp);
        }

        // Método para obtener OTPs expirados
        public List<StudentOTPDto> GetExpiredOTPs()
        {
            return _otpList.Where(otp => otp.ExpirationDate < DateTime.UtcNow).ToList();
        }

        // Método para obtener OTPs activos
        public List<StudentOTPDto> GetActiveOTPs()
        {
            return _otpList.Where(otp => otp.ExpirationDate >= DateTime.UtcNow).ToList();
        }

        // Método para eliminar un OTP de la lista
        public void RemoveOTP(StudentOTPDto otp)
        {
            _otpList.Remove(otp);
        }

        // Método para generar un OTP
        public string GenerateOTP()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Método para enviar la lista de OTPs activos a OTPCleanupService
        public void SendActiveOTPsToCleanupService()
        {
            var activeOTPs = GetActiveOTPs();
            _otpCleanupService.ReceiveActiveOTPs(activeOTPs);
        }
    }
}