using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Models;

namespace ClassNotes.API.Services.ConcurrentGroups
{
    public interface IAttendanceGroupCacheManager
    {
        void RegisterGroup(Guid courseId, AttendanceGroupCache groupCache);
        AttendanceGroupCache GetGroupCache(Guid courseId);
        TemporaryAttendanceEntry TryGetStudentEntryByEmail(Guid courseId, string email);
    }
}
