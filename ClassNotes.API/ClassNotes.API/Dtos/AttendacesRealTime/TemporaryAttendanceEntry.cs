using NetTopologySuite.Geometries;

namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class TemporaryAttendanceEntry
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }

        public string Otp {  get; set; }
        public string QrContent { get; set; }
        public DateTime ExpirationTime { get; set; }
        public Point GeolocationToCompare { get; set; }
        public bool IsCheckedIn { get; set; } = false;
        public string AttendanceMethod { get; set; } // OTP, "QR"
    }
}
