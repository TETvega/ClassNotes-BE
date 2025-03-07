using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.CourseSettings
{
	// --------------------- CP --------------------- //
	public class CourseSettingCreateDto
	{
		// Tipo de puntuación
		[Display(Name = "tipo de puntuación")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[StringLength(15, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string ScoreType { get; set; }

		// Fecha de inicio del periodo
		[Display(Name = "fecha de inicio")]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public DateTime StartDate { get; set; }

		// Fecha de finalización del periodo
		[Display(Name = "fecha de fin")]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public DateTime EndDate { get; set; }

		// Nota minima
		[Display(Name = "nota minima")]
		[Range(0, 100)]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public float MinimumGrade { get; set; }

		// Nota maxima
		[Display(Name = "nota maxima")]
		[Range(0, 100)]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public float MaximumGrade { get; set; }

		// Tiempo para enviar la asistencia
		[Display(Name = "tiempo maximo para enviar la asistencia")]
		[Range(5, 59)]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public int MinimumAttendanceTime { get; set; }
	}
}
