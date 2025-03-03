using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Services.Emails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/emails")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class EmailController : ControllerBase
	{
		private readonly IEmailsService _emailsService;

		public EmailController(IEmailsService emailsService)
        {
			this._emailsService = emailsService;
		}

        [HttpPost]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<EmailDto>>> Send(EmailDto dto)
		{
			var response = await _emailsService.SendEmailAsync(dto);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPost("send-pdf")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<IActionResult> SendEmailWithPdf([FromBody] EmailGradeDto dto)
		{
			var response = await _emailsService.SendEmailWithPdfAsync(dto);
			return StatusCode(response.StatusCode, response);
		}
	}
}
