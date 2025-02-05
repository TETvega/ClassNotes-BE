using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Otp;

namespace ClassNotes.API.Services.Otp
{
	public interface IOtpService
	{
		Task<ResponseDto<OtpDto>> CreateAndSendOtpAsync(OtpCreateDto dto);
		Task<ResponseDto<OtpDto>> ValidateOtpAsync(OtpValidateDto dto);
	}
}
