namespace ClassNotes.API.Dtos.Activities
{
    public class ActivityDto // Para el getAll
    {
        public Guid Id { get; set; } // Id de la actividad
        public string Name { get; set; } // Nombre de la actividad
        public bool IsExtra { get; set; } // Para ver si es de puntos extra o no
        public float MaxScore { get; set; } // Puntaje maximo de la actividad
        public DateTime QualificationDate { get; set; } // Fecha n que se piensa calificar
        public Guid TagActivityId { get; set; } // Id de su respectivo tag (como una categoria)

        // Relaciones
        public UnitInfo Unit { get; set; } // Debido a que se debe retornar info de la unidad
        public CourseInfo Course { get; set; } // se debe de retornar info del curso

        public class UnitInfo // Es una clase anidada con las propiedades que se quieren ver sobre la unidad al retornar una actividad
        {
            public Guid Id { get; set; }
            public int Number { get; set; } 
        }

        public class CourseInfo // Es una clase anidada con las propiedades que se quieren ver sobre la unidad al retornar una actividad
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}