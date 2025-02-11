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
		public int MaxScore { get; set; }	

		// Fecha no va ya que se va a manejar la actual al momento de calificar

		// Id del curso
		[Display(Name = "id del curso")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public int CourseId { get; set; }	
	}
}
