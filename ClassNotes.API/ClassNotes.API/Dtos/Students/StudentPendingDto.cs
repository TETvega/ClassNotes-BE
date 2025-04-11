namespace ClassNotes.API.Dtos.Courses
{
    public class StudentPendingDto
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string EMail { get; set; }
        public int PendingActivities { get; set; }
    }
}
