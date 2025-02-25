using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Users
{
	public class UserEditEmailDto
	{
		[Required(ErrorMessage = "La nueva dirección de correo electrónico es requerida.")]
		[EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
		public string NewEmail { get; set; }
	}
}
