namespace ClassNotes.API.Dtos.Activities
{
    // Se creo debido a que tanto el getById y el getAll se iba a manejar diferente información
    public class ActivitySummaryDto // Este es para el get by id
    {
        public Guid Id { get; set; } // Id de la actividad
        public string Name { get; set; } // Nombre de la actividad
        public DateTime QualificationDate { get; set; } // Fecha en que se planea evaluar
        public Guid TagActivityId { get; set; } // Id de su tag (como una categoria se podria decir)

        // Relaciones
        public CourseInfo Course { get; set; } // Para mostrar info del curso al que pertenece la actividad
        public Guid CenterId { get; set; } // Para mostrar el centro al que pertenece la actividad

        // Clase anidada para el curso
        public class CourseInfo // Clase anidada para asi mostrar info del curso al que pertenece la actividad
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}