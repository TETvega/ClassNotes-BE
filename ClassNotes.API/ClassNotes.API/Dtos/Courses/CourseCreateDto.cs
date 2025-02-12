using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Courses
{
    // --------------------- CP --------------------- //
	public class CourseCreateDto
	{
		// Nombre
		[Display(Name = "nombre")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[StringLength(50, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string Name { get; set; }

		// Sección
		[Display(Name = "sección")]
		[StringLength(4, ErrorMessage = "La {0} debe tener menos de {1} caracteres.")]
		public string Section { get; set; }

		// Codigo
		[Display(Name = "codigo")]
		[StringLength(15, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string Code { get; set; }

		// Activo?
		[Display(Name = "es activo")]
		[Required(ErrorMessage = "El campo {0} es requerido.")]
		public bool IsActive { get; set; }

		// Id del centro
		[Display(Name = "id del centro")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public Guid CenterId { get; set; }

		// Id de la configuración
		[Display(Name = "id de la configuración")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public Guid SettingId { get; set; }
	}
}
