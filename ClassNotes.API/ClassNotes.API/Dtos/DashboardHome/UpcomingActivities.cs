namespace ClassNotes.API.Dtos.DashboardHome
{
    public class UpcomingActivities
    {
        public Guid ActivityId { get; set; }
        public string ActivityName { get; set; }
        public DateTime QualificationDate { get; set; }

        public Guid CourseId { get; set; }
        public string CourseName { get; set;}

        public Guid CenterId { get; set; }
        public string CenterName { get; set; }
    }
}
