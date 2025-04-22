using ClassNotes.API.Dtos.Common;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Courses
{
	// --------------------- CP --------------------- //
	public class CourseCreateDto
	{
		// Datos del curso
		// Nombre
		[Display(Name = "nombre")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[StringLength(50, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string Name { get; set; }

		// Sección
		[Display(Name = "sección")]
		[StringLength(4, ErrorMessage = "La {0} debe tener menos de {1} caracteres.")]
		public string Section { get; set; }

		// Hora de inicio de la clase
		[Display(Name = "hora de inicio")]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public TimeSpan StartTime { get; set; }

		// Hora de finalización de la clase
		[Display(Name = "hora de finalización")]
		public TimeSpan? FinishTime { get; set; }

		// Codigo
		[Display(Name = "codigo")]
		[StringLength(15, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string Code { get; set; }

		// Id del centro
		[Display(Name = "id del centro")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public Guid CenterId { get; set; }

		// Id de la configuración
		[Display(Name = "id de la configuración")]
		// [Required(ErrorMessage = "El {0} es requerido.")] Ya no es requerido
		public Guid? SettingId { get; set; }
	}
}
