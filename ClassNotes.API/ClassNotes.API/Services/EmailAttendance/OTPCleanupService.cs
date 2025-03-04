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

        public OTPCleanupService(
            ILogger<OTPCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de limpieza de OTPs iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Iniciando limpieza de OTPs. Total de OTPs en la lista: {EmailAttendanceController.OTPList.Count}.");

                    if (EmailAttendanceController.OTPList.Count == 0)
                    {
                        _logger.LogInformation("No hay OTPs en la lista. El servicio esperará 1 minuto antes de verificar nuevamente.");
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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

                                var asistenciaExistente = await context.Attendances
                                    .AnyAsync(a => a.StudentId == otp.StudentId && a.CourseId == otp.CourseId);

                                if (!asistenciaExistente)
                                {
                                    var attendanceCreateDto = new AttendanceCreateDto
                                    {
                                        Attended = "No Presente",
                                        CourseId = otp.CourseId,
                                        StudentId = otp.StudentId
                                    };

                                    _logger.LogInformation($"Creando asistencia para el estudiante {otp.StudentId} en el curso {otp.CourseId}.");

                                    var attendanceDto = await attendanceService.CreateAttendanceAsync(attendanceCreateDto);

                                    _logger.LogInformation($"Asistencia creada con ID: {attendanceDto.Id}.");
                                }
                                else
                                {
                                    _logger.LogInformation($"El estudiante {otp.StudentId} ya tiene una asistencia registrada en el curso {otp.CourseId}.");
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

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("Servicio de limpieza de OTPs detenido.");
        }
    }
}