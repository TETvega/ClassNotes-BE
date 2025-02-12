using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Users
{
	public class UserEditPasswordOtpDto
	{
		[Required(ErrorMessage = "El correo electrónico es requerido.")]
		[EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
		public string Email { get; set; }

		[Required(ErrorMessage = "El código OTP es requerido.")]
		[RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El código OTP deben ser 6 números.")]
		public string OtpCode { get; set; }

		[Required(ErrorMessage = "La nueva contraseña es requerida.")]
		[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "La contraseña debe contener al menos 8 caracteres e incluir minúsculas, mayúsculas, números y caracteres especiales.")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "La confirmación de la nueva contraseña es requerida.")]
		[Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
		public string ConfirmPassword { get; set; }
    }
}
