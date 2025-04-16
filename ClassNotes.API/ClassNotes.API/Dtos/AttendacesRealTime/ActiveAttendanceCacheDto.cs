namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class ActiveAttendanceCacheDto
    {
        public Guid CourseId { get; set; }
        public bool StrictMode { get; set; }
        public string AttendanceMethod { get; set; }
        public DateTime Expiration { get; set; }

        public List<AttendanceStudentStatus> Students { get; set; }
    }
}
