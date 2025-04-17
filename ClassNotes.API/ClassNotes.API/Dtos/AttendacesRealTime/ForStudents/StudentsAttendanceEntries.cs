using ClassNotes.API.Constants;

namespace ClassNotes.API.Dtos.AttendacesRealTime.ForStudents
{
    public class StudentsAttendanceEntries
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StdentLastName { get; set; }
        public string Email { get; set; }
        public DateTime AttendanceEntryDatee { get; set; }
        public bool IsCheckedIn { get; set; } 
        public string Status { get; set; }
        public string AttendanceMethod { get; set; } // OTP, "QR"
        public string ChangeBy { get; set; }
    }
}
