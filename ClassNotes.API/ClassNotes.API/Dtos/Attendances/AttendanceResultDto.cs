public class AttendanceResultDto
{
    public Guid Id { get; set; }
    public bool Attended { get; set; } 
    public DateTime RegistrationDate { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
}