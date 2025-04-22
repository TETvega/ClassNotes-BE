namespace ClassNotes.API.Dtos.AttendacesRealTime.ForStudents
{
    public class StudentAttendanceResponse
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public double Distance { get; set; }
        public string Status { get; set; }
        public string Method { get; set; }
    }
}
