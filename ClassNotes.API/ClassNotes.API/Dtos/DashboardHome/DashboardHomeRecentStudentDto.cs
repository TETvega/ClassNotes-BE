namespace ClassNotes.API.Dtos.DashboardHome;

public class DashboardHomeRecentStudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }        //combinar FirstName y LastName
    public string CourseName { get; set; }
    public string CenterAbbreviation { get; set; }
    public string Email { get; set; }
}
