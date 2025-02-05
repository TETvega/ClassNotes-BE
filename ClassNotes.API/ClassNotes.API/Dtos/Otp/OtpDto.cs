namespace ClassNotes.API.Dtos.Otp
{
	public class OtpDto
	{
        public string Email { get; set; }
        public string OtpCode { get; set; }
        public int ExpirationSeconds { get; set; }
    }
}
