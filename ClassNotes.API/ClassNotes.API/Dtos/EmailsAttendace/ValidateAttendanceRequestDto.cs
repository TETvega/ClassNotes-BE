namespace ClassNotes.API.Dtos.EmailsAttendace
{
    public class ValidateAttendanceRequestDto
    {
        public string OTP { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
