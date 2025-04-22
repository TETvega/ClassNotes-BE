using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Users
{
	public class UserEditPasswordDto
	{
		[Required(ErrorMessage = "La contraseña actual es requerida.")]
		public string CurrentPassword { get; set; }

		[Required(ErrorMessage = "La nueva contraseña es requerida.")]
		[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "La contraseña debe contener al menos 8 caracteres e incluir minúsculas, mayúsculas, números y caracteres especiales.")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "La confirmación de la nueva contraseña es requerida.")]
		[Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
		public string ConfirmNewPassword { get; set; }
    }
}
