namespace ClassNotes.API.Dtos.EmailsAttendace
{
    public class StudentOTPDto
    {
        public string OTP { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid StudentId { get; set; }

        public string TeacherId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int RangoValidacionMetros { get; set; }
    }
}
