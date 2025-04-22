using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Models;
using System.Collections.Concurrent;

namespace ClassNotes.API.Services.ConcurrentGroups
{
    public class AttendanceGroupCacheManager: IAttendanceGroupCacheManager
    {
        private readonly ConcurrentDictionary<Guid, AttendanceGroupCache> _groupsCache;
        private readonly ILogger<AttendanceGroupCacheManager> _logger;

        public AttendanceGroupCacheManager(
            ConcurrentDictionary<Guid, AttendanceGroupCache> groupsCache,
            ILogger<AttendanceGroupCacheManager> logger)
        {
            _groupsCache = groupsCache;

            _logger = logger;
        }

        public void RegisterGroup(Guid courseId, AttendanceGroupCache groupCache)
        {
            _groupsCache[courseId] = groupCache;
            _logger.LogInformation($"[CACHE MANAGER] Registros en caché: {_groupsCache.Count}");
            _logger.LogInformation($"[CACHE MANAGER] Registro: {courseId} con {groupCache.Entries.Count} entradas");
            _logger.LogInformation($"[CACHE MANAGER] Registro: {courseId} expira en  {groupCache.ExpirationTime} , en {groupCache.ExpirationTime - DateTime.Now}");
        }

        public AttendanceGroupCache GetGroupCache(Guid courseId)
        {
            return _groupsCache.TryGetValue(courseId, out var group) ? group : null;
        }

        public TemporaryAttendanceEntry TryGetStudentEntryByEmail(Guid courseId, string email)
        {
            _logger.LogInformation($"[CACHE MANAGER] Intento de Acceso a GRUPO[{courseId}] -> entrada {email}, Registros Existentes {_groupsCache.Count}");
            if (!_groupsCache.TryGetValue(courseId, out var group))
                return null;
            _logger.LogInformation($"[CACHE MANAGER] Registro encontrado con exito {group} -> {group.Entries.Count()}");
            return group.Entries.FirstOrDefault(e => e.Email == email);
        }
    }
}
