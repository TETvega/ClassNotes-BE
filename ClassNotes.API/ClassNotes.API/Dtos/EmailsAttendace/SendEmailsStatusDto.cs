namespace ClassNotes.API.Dtos.EmailsAttendace
{
    public class SendEmailsStatusDto
    {
        public string StudentName { get; set; }
        public string Email { get; set; }

        public string OTP { get; set; }
        public bool SentStatus { get; set; }
        public string Message { get; set; }
    }
}
