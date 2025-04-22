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

		public TimeSpan? FinishTime { get; set; } // La hora a la que la clase termina 

		public string Code { get; set; } // El codigo de la clase

		public bool IsActive { get; set; } // Para poder ocultar la clase de la vista

		public Guid CenterId { get; set; } // El centro al que pertenece

		public Guid SettingId { get; set; } // Configuración globlal de la clase
	}
}
