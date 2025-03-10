using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassNotes.API.Controllers;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;

namespace ClassNotes.API.Services
{
    public class OTPCleanupService : BackgroundService
    {
        private readonly ILogger<OTPCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false; // Bandera para controlar si el servicio está en ejecución
        private bool _messageShown = false; // Bandera para controlar si el mensaje de inactividad ya se mostró

        public OTPCleanupService(
            ILogger<OTPCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        // Método para reactivar el servicio
        public void ReactivateService()
        {
            if (!_isRunning)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
                _messageShown = false; // Reiniciar la bandera del mensaje
                _logger.LogInformation("Servicio de limpieza de OTPs reactivado.");
            }
            else
            {
                _logger.LogInformation("El servicio de limpieza de OTPs ya está activo.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de limpieza de OTPs iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_isRunning)
                    {
                        if (!_messageShown)
                        {
                            _logger.LogInformation("El servicio de limpieza de OTPs está inactivo. Esperando reactivación...");
                            _messageShown = true; // Marcar que el mensaje ya se mostró
                        }
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Esperar 1 minuto antes de verificar nuevamente
                        continue;
                    }

                    _logger.LogInformation($"Iniciando limpieza de OTPs. Total de OTPs en la lista: {EmailAttendanceController.OTPList.Count}.");

                    if (EmailAttendanceController.OTPList.Count == 0)
                    {
                        _logger.LogInformation("No hay OTPs en la lista. El servicio se detendrá.");
                        _isRunning = false; // Desactivar el servicio
                        continue;
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                        var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();

                        var expiredOTPs = EmailAttendanceController.OTPList
                            .Where(otp => otp.ExpirationDate < DateTime.UtcNow)
                            .ToList();

                        _logger.LogInformation($"OTPs caducados encontrados: {expiredOTPs.Count}.");

                        foreach (var otp in expiredOTPs)
                        {
                            try
                            {
                                _logger.LogInformation($"Verificando asistencia para el estudiante {otp.StudentId} en el curso {otp.CourseId}.");

                                // Verificar si existe una asistencia "Presente" para el estudiante en el curso
                                var asistenciaPresente = await context.Attendances
                                    .AnyAsync(a => a.StudentId == otp.StudentId &&
                                                   a.CourseId == otp.CourseId &&
                                                   a.Attended == "Presente");

                                if (!asistenciaPresente)
                                {
                                    var attendanceCreateDto = new AttendanceCreateDto
                                    {
                                        Attended = "No Presente", // Marcar como "No Presente"
                                        CourseId = otp.CourseId,
                                        StudentId = otp.StudentId
                                    };

                                    _logger.LogInformation($"Creando asistencia para el estudiante {otp.StudentId} en el curso {otp.CourseId}.");

                                    var attendanceDto = await attendanceService.CreateAttendanceAsync(attendanceCreateDto);

                                    _logger.LogInformation($"Asistencia creada con ID: {attendanceDto.Id}.");
                                }
                                else
                                {
                                    _logger.LogInformation($"El estudiante {otp.StudentId} ya tiene una asistencia registrada como 'Presente' en el curso {otp.CourseId}.");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error al procesar el OTP para el estudiante {otp.StudentId}.");
                            }
                            finally
                            {
                                EmailAttendanceController.OTPList.Remove(otp);
                                _logger.LogInformation($"OTP para el estudiante {otp.StudentId} eliminado de la lista.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al limpiar OTPs caducados.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Servicio de limpieza de OTPs detenido.");
        }
    }
}