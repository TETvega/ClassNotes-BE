namespace ClassNotes.API.Dtos.Emails
{
    public class SMTPAcountDto
    {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public bool EnableSsl { get; set; }
    }
}
