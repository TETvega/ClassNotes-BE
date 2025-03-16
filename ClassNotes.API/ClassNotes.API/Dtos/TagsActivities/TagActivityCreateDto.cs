using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.TagsActivities
{
	public class TagActivityCreateDto
	{
		[Required(ErrorMessage = "Es requerido ingresar el nombre de la etiqueta.")]
		[MaxLength(15, ErrorMessage = "El nombre de la etiqueta debe ser menor a 15 caracteres.")]
		public string Name { get; set; }

		[RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "El código hexadecimal debe tener el formato correcto, por ejemplo, #FFFFFF.")]
		[Required(ErrorMessage = "Es requerido ingresar el código hexadecimal de la etiqueta.")]
		public string ColorHex { get; set; }

		[Required(ErrorMessage = "Es requerido ingresar un icono para la etiqueta.")]
		[MaxLength(20, ErrorMessage = "El nombre del icono debe ser menor a 20 caracteres.")]
		public string Icon { get; set; }
	}
}
