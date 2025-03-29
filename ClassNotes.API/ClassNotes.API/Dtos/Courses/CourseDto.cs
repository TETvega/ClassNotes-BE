namespace ClassNotes.API.Dtos.Courses
{
    // --------------------- CP --------------------- //
	public class CourseDto
	{
		// Campos del curso
		public Guid Id { get; set; }

		public string Name { get; set; } // Es el nombre del curso

		public string Section { get; set; } // La sección en la que esta programado el curso

		public TimeSpan StartTime { get; set; } // La hora a la que la clase inicia

		public TimeSpan FinishTime { get; set; } // La hora a la que la clase termina 

		public string Code { get; set; } // El codigo de la clase

		public bool IsActive { get; set; } // Para poder ocultar la clase de la vista

		public Guid CenterId { get; set; } // El centro al que pertenece

		public Guid SettingId { get; set; } // Configuración globlal de la clase


		

		// Campos de la configuración del curso
		public string SettingName { get; set; } // El nombre que se le dara a la configuración

		public string ScoreType { get; set; } // El tipo de puntuación por si es ponderado u oro

		public DateTime StartDate { get; set; } // Inicio de periodo

		public DateTime EndDate { get; set; } // Fin de periodo

		public float MinimumGrade { get; set; } // Nota minima en una clase

		public float MaximumGrade { get; set; } // Nota maxima en una clase

		public int MinimumAttendanceTime { get; set; } // El tiempo en el cual se debe de mandar la asistencia

		public bool IsOriginal { get; set; } // Para separar las configuraciones originales de las replicas (copias)
	}
}
