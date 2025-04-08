namespace ClassNotes.API.Dtos.DashboardHome
{
    public class ActiveClasses
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public string CourseSection { get;set; }

        public int ActiveStudentsCount { get; set; }
        public int TotalActivities {  get; set; }
        public int TotalActivitiesDone { get; set; }


        public Guid CenterId { get; set; }
        public string CenterName { get; set; }
        public string CenterAbb { get; set; }
        


    }
}
