namespace ClassNotes.API.Dtos.DashboardHome;

public class DashboardHomeActiveCourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string CenterAbbreviation { get; set; }
    public int StudentsCount { get; set; }
    public float Average { get; set; }
    public int CompletedActivitiesCount { get; set; }
    public int ActivitiesCount { get; set; }
}
