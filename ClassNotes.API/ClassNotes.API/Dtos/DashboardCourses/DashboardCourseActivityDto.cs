namespace ClassNotes.API.Dtos.DashboardCourses
{
    // --------------------- CP --------------------- //
    public class DashboardCourseActivityDto
    {
        public Guid Id { get; set; } // Id de la actividad
        public string Name { get; set; } // Nombre/descripción de la actividad 
        public DateTime QualificationDate { get; set; } // Fecha en la que se piensa calificar
    }
}