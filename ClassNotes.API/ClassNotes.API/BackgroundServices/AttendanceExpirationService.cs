using ClassNotes.API.Constants;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Database;
using ClassNotes.API.Hubs;
using ClassNotes.API.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ClassNotes.API.BackgroundServices
{
    public class AttendanceExpirationService:BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Guid, AttendanceGroupCache> _groupCache;
        private readonly ILogger<AttendanceExpirationService> _logger;

        public AttendanceExpirationService(
            IServiceProvider serviceProvider,
            ConcurrentDictionary<Guid, AttendanceGroupCache> groupCache,
            ILogger<AttendanceExpirationService> logger
            )
        {
            _serviceProvider = serviceProvider;
            _groupCache = groupCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var expiredGroups = _groupCache
                    .Where(g => g.Value.ExpirationTime <= now)
                    .Select(g => g.Key)
                    .ToList();
                _logger.LogInformation("Ejecutando servicio en Segundo Plano ");
                foreach (var groupId in expiredGroups)
                {
                    if (_groupCache.TryRemove(groupId, out var group))
                    {
                        _logger.LogInformation($"{groupId} , Eliminando Datos");
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<AttendanceHub>>();

                        foreach (var entry in group.Entries.Where(e => !e.IsCheckedIn))
                        {
                            var attendance = new AttendanceEntity
                            {
                                CourseId = entry.CourseId,
                                StudentId = entry.StudentId,
                                Attended = false,
                                Status = MessageConstant_Attendance.NOT_PRESENT,
                                RegistrationDate = DateTime.UtcNow,
                                CreatedBy = group.UserId,
                                CreatedDate = DateTime.UtcNow,
                                Method = Attendance_Helpers.TYPE_MANUALLY,
                                ChangeBy = Attendance_Helpers.SYSTEM
                            };

                            db.Attendances.Add(attendance);

                            await hub.Clients.Group(entry.CourseId.ToString())
                                .SendAsync(Attendance_Helpers.UPDATE_ATTENDANCE_STATUS, new
                                {
                                    studentId = entry.StudentId,
                                    status = MessageConstant_Attendance.NOT_PRESENT
                                });
                        }

                        await db.SaveChangesWithoutAuditAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
