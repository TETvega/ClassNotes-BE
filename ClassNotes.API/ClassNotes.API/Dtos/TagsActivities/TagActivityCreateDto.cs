using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.TagsActivities
{
	public class TagActivityCreateDto
	{
		[Required(ErrorMessage = "Es requerido ingresar el nombre de la etiqueta.")]
		[StringLength(20)]
		public string Name { get; set; }

		[RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "El código hexadecimal debe tener el formato correcto, por ejemplo, #FFFFFF.")]
		[Required(ErrorMessage = "Es requerido ingresar el código hexadecimal de la etiqueta.")]
		[StringLength(7)]
		public string ColorHex { get; set; }

		[Required(ErrorMessage = "Es requerido ingresar un icono para la etiqueta.")]
		[StringLength(50)]
		public string Icon { get; set; }

		[Required(ErrorMessage = "Es requerido ingresar el ID del docente.")]
		[StringLength(450)]
		public string TeacherId { get; set; }
	}
}
