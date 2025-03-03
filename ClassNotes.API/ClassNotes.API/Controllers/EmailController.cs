using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Services.Emails;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[Route("api/emails")]
	[ApiController]
	public class EmailController : ControllerBase
	{
		private readonly IEmailsService _emailsService;

		public EmailController(IEmailsService emailsService)
        {
			this._emailsService = emailsService;
		}

        [HttpPost]
		public async Task<ActionResult<ResponseDto<EmailDto>>> Send(EmailDto dto)
		{
			var response = await _emailsService.SendEmailAsync(dto);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPost("send-pdf")]
		public async Task<IActionResult> SendEmailWithPdf([FromBody] EmailGradeDto dto)
		{
			var response = await _emailsService.SendEmailWithPdfAsync(dto);
			return StatusCode(response.StatusCode, response);
		}
	}
}
