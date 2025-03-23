using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Dtos.EmailsAttendace;

namespace ClassNotes.API.Services
{
    public class OTPCleanupService : BackgroundService
    {
        private readonly ILogger<OTPCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false; // Bandera para controlar si el servicio está en ejecución
        private bool _messageShown = false; // Bandera para controlar si el mensaje de inactividad ya se mostró

        // Lista de OTPs activos recibidos desde EmailAttendanceService
        private List<StudentOTPDto> _activeOTPs = new List<StudentOTPDto>();

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

        // Método para recibir la lista de OTPs activos
        public void ReceiveActiveOTPs(List<StudentOTPDto> activeOTPs)
        {
            _activeOTPs = activeOTPs;
            ReactivateService(); // Reactivar el servicio si estaba pausado
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

                    // Mostrar el número de OTPs activos cada minuto
                    _logger.LogInformation($"OTPs activos en la lista de espera: {_activeOTPs.Count}.");

                    // Verificar si hay OTPs en espera
                    if (_activeOTPs.Count == 0)
                    {
                        _logger.LogInformation("No hay OTPs en la lista de espera. Realizando segunda verificación...");

                        // Esperar un breve momento antes de la segunda verificación
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                        // Segunda verificación: obtener OTPs activos nuevamente
                        if (_activeOTPs.Count == 0)
                        {
                            // Segunda verificación confirma que no hay OTPs activos
                            _logger.LogInformation("Segunda verificación: no hay OTPs activos. Pausando el servicio...");
                            _isRunning = false; // Pausar el servicio
                            continue;
                        }
                        else
                        {
                            _logger.LogInformation($"Segunda verificación: {_activeOTPs.Count} OTPs activos encontrados. Continuando el proceso...");
                        }
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                        var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();

                        // Obtener OTPs caducados
                        var expiredOTPs = _activeOTPs
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
                                // Eliminar el OTP de la lista de espera
                                _activeOTPs.Remove(otp);
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