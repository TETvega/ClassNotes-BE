using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Models;
using System.Collections.Concurrent;

namespace ClassNotes.API.Services.ConcurrentGroups
{
    public class AttendanceGroupCacheManager: IAttendanceGroupCacheManager
    {
        private readonly ConcurrentDictionary<Guid, AttendanceGroupCache> _groupsCache = new();
        private readonly ILogger<AttendanceGroupCacheManager> _logger;

        public AttendanceGroupCacheManager(ILogger<AttendanceGroupCacheManager> logger)
        {
            _logger = logger;
        }

        public void RegisterGroup(Guid courseId, AttendanceGroupCache groupCache)
        {
            _groupsCache[courseId] = groupCache;
            _logger.LogInformation($"Grupo registrado: {courseId} con {groupCache.Entries.Count} estudiantes");
        }

        public AttendanceGroupCache GetGroupCache(Guid courseId)
        {
            return _groupsCache.TryGetValue(courseId, out var group) ? group : null;
        }

        public TemporaryAttendanceEntry TryGetStudentEntryByEmail(Guid courseId, string email)
        {
            if (!_groupsCache.TryGetValue(courseId, out var group))
                return null;

            return group.Entries.FirstOrDefault(e => e.Email == email);
        }
    }
}
