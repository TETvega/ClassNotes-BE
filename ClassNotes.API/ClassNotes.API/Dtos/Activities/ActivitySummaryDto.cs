namespace ClassNotes.API.Dtos.Activities
{
    public class ActivitySummaryDto 
    {
        public Guid Id { get; set; } // Id de la actividad
        public string Name { get; set; } // Nombre de la actividad
        public string Description { get; set; }
        public DateTime QualificationDate { get; set; } // Fecha en que se planea evaluar
        public Guid TagActivityId { get; set; } // Id de su tag (como una categoria se podria decir)

        // Relaciones
        public Guid CourseId { get; set; } // Para mostrar info del curso al que pertenece la actividad
        public string CourseName { get; set; }
        public Guid CenterId { get; set; } // Para mostrar el centro al que pertenece la actividad
        public string CenterName { get; set; }
        public string CenterAbb { get; set; }

    }
}