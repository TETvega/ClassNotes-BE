using System.Security.Claims;
using ClassNotes.API.Dtos.Auth;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.Auth
{
	public interface IAuthService
	{
		// --------------------- CP --------------------- //
		
		Task<ResponseDto<LoginResponseDto>> LoginAsync(LoginDto dto);

		Task<ResponseDto<LoginResponseDto>> RegisterAsync(RegisterDto dto);

		Task<ResponseDto<LoginResponseDto>> RefreshTokenAsync(RefreshTokenDto dto);
		ClaimsPrincipal GetTokenPrincipal(string token);

		// --------------------- CP --------------------- //
	}
}
