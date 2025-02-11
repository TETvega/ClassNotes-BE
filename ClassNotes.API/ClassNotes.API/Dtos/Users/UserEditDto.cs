using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Users
{
	public class UserEditDto
	{
		[Required(ErrorMessage = "El campo de primer nombre es requerido.")]
		[MinLength(3, ErrorMessage = "El primer nombre debe tener almenos 3 caracteres.")]
		public string FirstName { get; set; }

		[Required(ErrorMessage = "El campo de segundo nombre es requerido.")]
		[MinLength(3, ErrorMessage = "El segundo nombre debe tener almenos 3 caracteres.")]
		public string LastName { get; set; }
	}
}
