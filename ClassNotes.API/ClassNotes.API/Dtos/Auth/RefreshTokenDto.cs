using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Auth
{
	public class RefreshTokenDto
	{
		[Required(ErrorMessage = "El Token es requerido.")]
		public string Token { get; set; }

		[Required(ErrorMessage = "El Refresh Token es requerido.")]
		public string RefreshToken { get; set; }
	}
}
