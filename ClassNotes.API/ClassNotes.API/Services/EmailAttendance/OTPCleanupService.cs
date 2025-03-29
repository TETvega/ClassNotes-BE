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
using System.Diagnostics;
using ClassNotes.API.Database.Entities;
using static ClassNotes.API.Database.ClassNotesContext;
using ClassNotes.API.Services.Audit;

namespace ClassNotes.API.Services
{
    public class OTPCleanupService : BackgroundService
    {
        private readonly ILogger<OTPCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private bool _messageShown = false;

        private List<StudentOTPDto> _activeOTPs = new List<StudentOTPDto>();

        public OTPCleanupService(
            ILogger<OTPCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void ReactivateService()
        {
            if (!_isRunning)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
                _messageShown = false;
                _logger.LogInformation("Servicio de limpieza de OTPs reactivado.");
            }
            else
            {
                _logger.LogInformation("El servicio de limpieza de OTPs ya está activo.");
            }
        }

        public void ReceiveActiveOTPs(List<StudentOTPDto> activeOTPs)
        {
            _activeOTPs = activeOTPs;
            ReactivateService();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de limpieza de OTPs iniciado.");

            var sysTemUserId = Guid.Parse("33907437-d1f8-41f9-9449-518a7700f4b0");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_isRunning)
                    {
                        if (!_messageShown)
                        {
                            _logger.LogInformation("El servicio de limpieza de OTPs está inactivo. Esperando reactivación...");
                            _messageShown = true;
                        }
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation($"OTPs activos en la lista de espera: {_activeOTPs.Count}.");

                    if (_activeOTPs.Count == 0)
                    {
                        _logger.LogInformation("No hay OTPs en la lista de espera. Realizando segunda verificación...");
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                        if (_activeOTPs.Count == 0)
                        {
                            _logger.LogInformation("Segunda verificación: no hay OTPs activos. Pausando el servicio...");
                            _isRunning = false;
                            continue;
                        }
                        else
                        {
                            _logger.LogInformation($"Segunda verificación: {_activeOTPs.Count} OTPs activos encontrados. Continuando el proceso...");
                        }
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var innerContext = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

                        var expiredOTPs = _activeOTPs
                            .Where(otp => otp.ExpirationDate < DateTime.UtcNow)
                            .ToList();

                        foreach (var otp in expiredOTPs)
                        {
                            try
                            {
                                using (var operationScope = _serviceProvider.CreateScope())
                                {
                                    var context = operationScope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                                    var auditService = operationScope.ServiceProvider.GetRequiredService<IAuditService>();

                                    // Desactivar auditoría temporalmente
                                    var originalAuditState = auditService.DisableAuditTemporarily();

                                    try
                                    {
                                        var exists = await context.Attendances
                                            .AsNoTracking()
                                            .AnyAsync(a => a.StudentId == otp.StudentId
                                                        && a.CourseId == otp.CourseId
                                                        && a.Attended == true);

                                        if (!exists)
                                        {
                                            var teacherRef = new UserEntity { Id = otp.TeacherId };
                                            context.Users.Attach(teacherRef);

                                            var attendance = new AttendanceEntity
                                            {
                                                Attended = false,
                                                Status = "No Presente",
                                                RegistrationDate = DateTime.UtcNow,
                                                CourseId = otp.CourseId,
                                                StudentId = otp.StudentId,
                                                CreatedByUser = teacherRef,
                                                UpdatedByUser = teacherRef
                                            };

                                            context.Attendances.Add(attendance);
                                            await context.SaveChangesAsync();
                                            _logger.LogInformation($"Asistencia creada para {otp.StudentId}");
                                        }
                                    }
                                    finally
                                    {
                                        auditService.RestoreAuditState(originalAuditState);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error procesando OTP para {otp.StudentId}");
                            }
                            finally
                            {
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