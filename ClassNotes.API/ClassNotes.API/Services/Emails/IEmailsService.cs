using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;

namespace ClassNotes.API.Services.Emails
{
	public interface IEmailsService
	{
		Task<ResponseDto<EmailDto>> SendEmailAsync(EmailDto dto);
		Task<ResponseDto<EmailDto>> SendEmailWithPdfAsync(EmailGradeDto dto);
	}
}
