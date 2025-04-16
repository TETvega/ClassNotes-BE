namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class AttendanceStudentStatus
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool Attendend {  get; set; } = false;
        public string Status { get; set; } /// <see cref="\ClassNotes.API\ClassNotes.API\Constants\MessageConstant_Attendance.cs"/>
    }
}
