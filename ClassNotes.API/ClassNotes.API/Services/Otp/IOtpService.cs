using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Otp;

namespace ClassNotes.API.Services.Otp
{
	public interface IOtpService
	{
		Task<ResponseDto<OtpGenerateResponseDto>> CreateAndSendOtpAsync(OtpCreateDto dto);
		Task<ResponseDto<OtpDto>> ValidateOtpAsync(OtpValidateDto dto);

		// CG: este servicio solo funciona para validar que el otp se elimina de cache
		Task<ResponseDto<OtpDto>> GetCachedOtpAsync(string email);
	}
}
