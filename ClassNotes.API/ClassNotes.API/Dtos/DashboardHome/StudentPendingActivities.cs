namespace ClassNotes.API.Dtos.DashboardHome
{
    public class StudentPendingActivities
    {
        public Guid StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string StudentEmail { get; set; }

        public int StudentActiveClasesCount { get; set; }
        public int StudentPendingActivitiesCount { get; set; }


    }
}
