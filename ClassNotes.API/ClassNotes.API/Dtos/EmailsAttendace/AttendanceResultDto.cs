namespace ClassNotes.API.Dtos.EmailsAttendace
{
    public class AttendanceResultDto
    {
        public string StudentName { get; set; }
        public Guid CourseId { get; set; }
        public DateTime ValidationTime { get; set; }
    }
}
