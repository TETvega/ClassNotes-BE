namespace ClassNotes.API.Dtos.DashboardHome;

public class DashboardHomeCenterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Abbreviation { get; set; }
    public string Logo { get; set; }
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
}
