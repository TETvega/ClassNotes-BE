namespace ClassNotes.API.Dtos.DashboardHome;

public class DashboardHomeStudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }        //combinar FirstName y LastName
    public string Email { get; set; }
    public int CoursesCount { get; set; }        //cantidad de clases con las que tiene al estudiante
    public int PendingsCount { get; set; }       //cantidad de actividades que falta calificarle
}
