using ClassNotes.API.Constants;
using NetTopologySuite.Geometries;

namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class TemporaryAttendanceEntry
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public string Email { get; set; }

        public string Otp {  get; set; }
        public string QrContent { get; set; }
        public DateTime ExpirationTime { get; set; }
        public float GeolocationLatitud { get; set; }
        public float GeolocationLongitud { get; set; }
        public bool IsCheckedIn { get; set; } = false;

        public string Status { get; set; } = $"{MessageConstant_Attendance.WAITING}";
        public string AttendanceMethod { get; set; } // OTP, "QR"
    }
}
