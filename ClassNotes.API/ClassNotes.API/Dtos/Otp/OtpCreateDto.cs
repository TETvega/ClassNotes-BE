using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Otp
{
	public class OtpCreateDto
	{
		[Required(ErrorMessage = "El correo electrónico es requerido.")]
		[EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
		public string Email { get; set; }
	}
}
