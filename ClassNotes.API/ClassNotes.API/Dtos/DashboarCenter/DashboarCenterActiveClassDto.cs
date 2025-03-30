namespace ClassNotes.API.Dtos.DashboarCenter
{
    public class DashboarCenterActiveClassDto
    {
        public Guid IdCourse { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int StudentCount { get; set; }
        public double AverageAttendance { get; set; }
        public double AverageScore { get; set; }
        public DashboardCenterActivityStatusDto ActivityStatus { get; set; }
    }
}
