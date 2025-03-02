namespace ClassNotes.API.Dtos.Otp
{
	public class OtpDto
	{
		public string UserId { get; set; }
		public string Email { get; set; }
		public int ExpirationSeconds { get; set; }

		//public string OtpCode { get; set; }
	}
}
