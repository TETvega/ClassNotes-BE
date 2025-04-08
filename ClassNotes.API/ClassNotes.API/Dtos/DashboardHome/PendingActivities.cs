namespace ClassNotes.API.Dtos.DashboardHome
{
    public class PendingActivities
    {
        public int PendingActivitiesCount { get; set; }

        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }

    }
}
