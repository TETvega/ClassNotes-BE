using System.Collections.Generic;
using System.Threading.Tasks;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services
{
    public interface IEmailAttendanceService
    {
        Task<ResponseDto<List<SendEmailsStatusDto>>> SendEmailsAsync(EmailAttendanceRequestDto request);
        Task ValidateAttendanceAsync(ValidateAttendanceRequestDto request);
        void AddOTP(StudentOTPDto otp);
        List<StudentOTPDto> GetExpiredOTPs();
        List<StudentOTPDto> GetActiveOTPs();
        void RemoveOTP(StudentOTPDto otp);
        string GenerateOTP();
        void SendActiveOTPsToCleanupService();
    }
}