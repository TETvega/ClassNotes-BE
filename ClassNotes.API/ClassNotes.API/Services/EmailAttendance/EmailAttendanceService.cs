
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
using CloudinaryDotNet;
using ClassNotes.API.Dtos.Attendances.Student;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Constants;

namespace ClassNotes.API.Services
{
    public class EmailAttendanceService : IEmailAttendanceService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ClassNotesContext _context;
        private readonly IEmailsService _emailsService;
        private readonly ILogger<EmailAttendanceService> _logger;
        private readonly DistanceService _distanceService;
        private readonly IAttendancesService _attendanceService;
        private readonly OTPCleanupService _otpCleanupService;
        private readonly IHubContext<AttendanceHub> _hubContext;
        private readonly EmailScheduleService _emailScheduleService;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(7); // Límite de tareas concurrentes

        // Lista privada para almacenar los OTPs
        private static List<StudentOTPDto> _otpList = new List<StudentOTPDto>();

        public EmailAttendanceService(
            IServiceScopeFactory scopeFactory,
            ClassNotesContext context,
            IEmailsService emailsService,
            ILogger<EmailAttendanceService> logger,
            DistanceService distanceService,
            IAttendancesService attendanceService,
            OTPCleanupService otpCleanupService,
            IHubContext<AttendanceHub> hubContext,
            EmailScheduleService emailScheduleService)
        {
            _scopeFactory = scopeFactory;
            _context = context;
            _emailsService = emailsService;
            _logger = logger;
            _distanceService = distanceService;
            _attendanceService = attendanceService;
            _otpCleanupService = otpCleanupService;
            _hubContext = hubContext;
            _emailScheduleService = emailScheduleService;

        }

        private void ValidarParametros(EmailAttendanceRequestDto request)
        {
            ValidateRange(request.RangoValidacionMetros, 15, 100, "El rango de validación debe estar entre 15 y 100 metros.");
            ValidateRange(request.TiempoExpiracionOTPMinutos, 1, 30, "El tiempo de expiración del OTP debe estar entre 1 y 30 minutos.");
        }

        private void ValidateRange(int value, int min, int max, string errorMessage)
        {
            if (value < min || value > max)
                throw new ArgumentException(errorMessage);
        }

        public async Task<ResponseDto<List<SendEmailsStatusDto>>> SendEmailsAsync(EmailAttendanceRequestDto request)
        {

            ValidarParametros(request);  
                var clase = await _context.Courses
                    .Include(c => c.Students)
                        .ThenInclude(sc => sc.Student)
                    .Include(c => c.Center)
                    .Where(c => c.Id == request.ClaseId &&
                                c.CenterId == request.CentroId &&
                                c.Center.TeacherId == request.ProfesorId)
                    .FirstOrDefaultAsync()
                    ?? throw new ArgumentException("No tienes permisos para esta clase o no existe.");

                if (!clase.Students.Any())
                    throw new ArgumentException("No hay estudiantes asociados a esta clase.");

                var estudiantes = clase.Students.Select(sc => sc.Student).ToList();

                var tasks = new List<Task>();
                var emailStatuses = new List<SendEmailsStatusDto>();  // Lista para almacenar el estado del envío


            foreach (var estudiante in estudiantes)
            {
                if (string.IsNullOrEmpty(estudiante.Email))
                {
                    _logger.LogWarning($"El estudiante {estudiante.FirstName} no tiene un correo electrónico válido.");
                    emailStatuses.Add(new SendEmailsStatusDto
                    {
                        StudentName = estudiante.FirstName,
                        Email = estudiante.Email,
                        SentStatus = false,
                        Message = "Correo electrónico no válido."
                    });
                    continue;
                }

                var otp = GenerateOTP();

                var studentOTP = new StudentOTPDto
                {
                    OTP = otp,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    StudentId = estudiante.Id,
                    StudentName = estudiante.FirstName,
                    StudentEmail = estudiante.Email,
                    CourseId = clase.Id,
                    TeacherId = $"{request.ProfesorId}",
                    ExpirationDate = DateTime.UtcNow.AddMinutes(request.TiempoExpiracionOTPMinutos),
                    RangoValidacionMetros = request.RangoValidacionMetros
                };

                AddOTP(studentOTP);

                var emailDto = CreateEmailDto(estudiante, clase, otp, request.TiempoExpiracionOTPMinutos);

                // Agregar la tarea de envío de correo
                // Crear un scope para cada tarea
                var task = Task.Run(async () =>
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailsService>();
                        await SendEmailAndNotifyAsync(emailDto, estudiante.Email, estudiante.FirstName, otp, emailStatuses, scopedEmailService);
                    }
                });

                tasks.Add(task);
            }

            // Esperar a que todas las tareas se completen
            await Task.WhenAll(tasks);

            // Limpiar OTPS activos
            SendActiveOTPsToCleanupService();

            // Programar el envío automático de correos si es necesario
            if (request.EnvioAutomatico)
            {
                _emailScheduleService.AddOrUpdateConfig(
                    request.ClaseId,
                    true,
                    TimeSpan.Parse(request.HoraEnvio),
                    request.DiasEnvio.Select(d => (DayOfWeek)d).ToList()
                );
            }

            // Retornar la respuesta con el estado del envío de correos
            return new ResponseDto<List<SendEmailsStatusDto>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.EMAIL_SENT_SUCCESSFULLY,
                Data = emailStatuses
            };
        }

        private EmailDto CreateEmailDto(StudentEntity estudiante, CourseEntity clase, string otp, int tiempoExpiracion)
        {
            return new EmailDto
            {
                To = estudiante.Email,
                Subject = "📌 Código de Validación de Asistencia",
                Content = $@"
            <div style='font-family: Arial, sans-serif; text-align: center;'>
                <h2 style='color: #4A90E2;'>👋 Hola {estudiante.FirstName},</h2>
                <p style='font-size: 16px; color: #333;'>
                    Para validar tu asistencia a la clase <strong>{clase.Name}</strong>, usa el siguiente código:
                </p>
                <div style='display: inline-block; background: #EAF3FF; padding: 15px; border-radius: 8px; font-size: 24px; font-weight: bold; letter-spacing: 3px;'>
                    {otp}
                </div>
                <p style='margin-top: 20px;'>
                    O puedes hacer clic en el siguiente botón para validar tu asistencia automáticamente:
                </p>
                <a href='https://tudominio.com/asistencia/{otp}' 
                   style='display: inline-block; background: #4A90E2; color: white; padding: 10px 20px; 
                          text-decoration: none; border-radius: 5px; font-size: 18px;'>
                    ✅ Validar Asistencia
                </a>
                <p style='font-size: 14px; color: #777; margin-top: 20px;'>
                    Este código es válido por {tiempoExpiracion} minutos.
                </p>
            </div>"
            };
        }

        private async Task SendEmailAndNotifyAsync(
            EmailDto emailDto,
            string email,
            string studentName,
            string otp,
            List<SendEmailsStatusDto> emailStatuses,
            IEmailsService scopedEmailService)
        {
            // Esperar a que el semáforo permita ejecutar la tarea
            await _semaphore.WaitAsync();

            try
            {
                // Enviar el correo y registrar el estado
                var result = await _emailsService.SendEmailAsync(emailDto);

                // Registrar el estado del envío del correo
                var emailStatus = new SendEmailsStatusDto
                {
                    StudentName = studentName,
                    Email = email,
                    OTP = otp,
                    SentStatus = result.Status,
                    Message = result.Status ? "Correo enviado correctamente." : result.Message
                };

                // Añadir el estado al listado de resultados
                emailStatuses.Add(emailStatus);

                // Si el correo fue enviado correctamente, notificar en tiempo real
                if (result.Status)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveEmailSent", new
                    {
                        StudentName = studentName,
                        Email = email,
                        OTP = otp
                    });
                }
            }
            finally
            {
                // Liberar el semáforo
                _semaphore.Release();
            }
        }


        public async Task ValidateAttendanceAsync(ValidateAttendanceRequestDto request)
        {
            try
            {
                // Buscar el OTP en la lista privada y verificar su validez
                var studentOTP = _otpList.FirstOrDefault(sotp => sotp.OTP == request.OTP);

                if (studentOTP == null || studentOTP.ExpirationDate < DateTime.UtcNow)
                {
                    throw new ArgumentException("OTP inválido o expirado.");
                }

                // Calcular la distancia solo si el OTP es válido (optimización por condiciones previas)
                var distance = _distanceService.CalcularDistancia(
                    request.Latitude, request.Longitude,
                    studentOTP.Latitude, studentOTP.Longitude);

                if (distance > studentOTP.RangoValidacionMetros)
                {
                    throw new ArgumentException($"Debes estar dentro de un radio de {studentOTP.RangoValidacionMetros} metros para validar tu asistencia.");
                }

                // Agrupar la obtención de estudiante y creación de DTO en una sola operación
                var estudiante = await _context.Students
                    .Where(s => s.Id == studentOTP.StudentId)
                    .Select(s => new { s.FirstName, s.Email, s.Id })
                    .FirstOrDefaultAsync();

                if (estudiante == null)
                {
                    throw new ArgumentException("Estudiante no encontrado.");
                }

                // Crear el DTO de asistencia
                var attendanceCreateDto = new AttendanceCreateDto
                {
                    Attended = true,
                    Status = "Presente",
                    CourseId = studentOTP.CourseId,
                    StudentId = studentOTP.StudentId,
                    TeacherId = studentOTP.TeacherId,
                };

                // Usar un Task.WhenAll para crear la asistencia y enviar la notificación al mismo tiempo
                var createAttendanceTask = _attendanceService.CreateAttendanceAsync(attendanceCreateDto);
                var sendNotificationTask = _hubContext.Clients.All.SendAsync("ReceiveAttendanceValidation", new
                {
                    StudentName = estudiante.FirstName,
                    Email = estudiante.Email,
                    AttendanceStatus = "Presente",
                    ValidationTime = DateTime.UtcNow
                });

                // Eliminar el OTP de la lista privada después de crear la asistencia y enviar la notificación
                RemoveOTP(studentOTP);

                // Esperar a que ambas tareas finalicen
                await Task.WhenAll(createAttendanceTask, sendNotificationTask);
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
