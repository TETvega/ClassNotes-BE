namespace ClassNotes.API.Dtos.Courses
{
    public class PendingClassesDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public Guid CenterId { get; set; }
        public string CenterName { get; set; }
        public int PendingActivities { get; set; }
    }
}
