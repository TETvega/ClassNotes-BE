using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Auth
{
	public class RegisterDto
	{
		[Display(Name = "Primer Nombre")]
		[Required(ErrorMessage = "El campo {0} es requerido.")]
		public string FirstName { get; set; }

		[Display(Name = "Segundo Nombre")]
		[Required(ErrorMessage = "El campo {0} es requerido.")]
		public string LastName { get; set; }

		[Display(Name = "Correo Electrónico")]
		[Required(ErrorMessage = "El campo {0} es requerido.")]
		[EmailAddress(ErrorMessage = "El campo {0} no es válido.")]
		public string Email { get; set; }

		[Display(Name = "Contraseña")]
		[Required(ErrorMessage = "El campo {0} es requerido.")]
		[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "La {0} debe contener al menos 8 caracteres e incluir minúsculas, mayúsculas, números y caracteres especiales.")]
		public string Password { get; set; }

		[Display(Name = "Confirmar Contraseña")]
		[Required(ErrorMessage = "El campo {0} es requerido.")]
		[Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
		public string ConfirmPassword { get; set; }
	}
}
