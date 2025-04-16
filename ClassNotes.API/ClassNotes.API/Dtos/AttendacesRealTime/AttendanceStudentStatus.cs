namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class AttendanceStudentStatus
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Status { get; set; } /// <see cref="\ClassNotes.API\ClassNotes.API\Constants\MessageConstant_Attendance.cs"/>
    }
}
