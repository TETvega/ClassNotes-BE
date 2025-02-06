using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Otp;
using ClassNotes.API.Services.Otp;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[Route("api/otp")]
	[ApiController]
	public class OtpController : ControllerBase
	{
		private readonly IOtpService _otpService;

		public OtpController(IOtpService otpService)
        {
			this._otpService = otpService;
		}

		[HttpPost("generate")]
		public async Task<ActionResult<ResponseDto<OtpDto>>> GenerateOtp(OtpCreateDto dto)
		{
			var response = await _otpService.CreateAndSendOtpAsync(dto);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPost("validate")]
		public async Task<ActionResult<ResponseDto<OtpDto>>> ValidateOtp(OtpValidateDto dto)
		{
			var response = await _otpService.ValidateOtpAsync(dto);
			return StatusCode(response.StatusCode, response);
		}

		[HttpGet("cache/{email}")]
		public async Task<ActionResult<ResponseDto<OtpDto>>> GetCachedOtp(string email)
		{
			var response = await _otpService.GetCachedOtpAsync(email);
            return StatusCode(response.StatusCode, response);
        }

    }
}
