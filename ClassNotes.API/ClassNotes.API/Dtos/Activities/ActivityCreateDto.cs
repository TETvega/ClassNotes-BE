using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Activities
{
	// --------------------- CP --------------------- //
	public class ActivityCreateDto
	{
		// Nombre
		[Display(Name = "nombre")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[StringLength(50, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string Name { get; set; }

		// Parcial
		[Display(Name = "parcial")]
		// [Required(ErrorMessage = "El {0} es requerido.")]
		public int GradingPeriod { get; set; }

		// Puntuación máxima
		[Display(Name = "puntuación máxima")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[Range(0,100, ErrorMessage = "La {0} debe estar entre {1} y {2}")] // Para validar que no haya notas menores a 0, ni mayores a 100
		public int MaxScore { get; set; }	

		// Fecha de calificación sera la fecha en la que se piensa calificar la actividad
		[Display(Name = "fecha de calificación")]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public DateTime QualificationDate { get; set; }

		// Id del curso
		[Display(Name = "id del curso")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public Guid CourseId { get; set; }	
	}
}
