using ClassNotes.API.Dtos.AttendacesRealTime;

namespace ClassNotes.API.Models
{
    public class AttendanceGroupCache
    {
        public DateTime ExpirationTime { get; set; }
        public string UserId { get; set; }
        public List<TemporaryAttendanceEntry> Entries { get; set; } = new();
    }
}
