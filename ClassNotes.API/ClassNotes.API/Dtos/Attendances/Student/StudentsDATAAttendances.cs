namespace ClassNotes.API.Dtos.Attendances.Student
{
    public class StudentsDATAAttendances
    {
        public bool Attendance { get; set; }
        public string Status { get; set; }
        public string AttendaceMethod { get; set; }
        public string LastChangeBy { get; set; }

        public ExtendedDateDto RegisterDate { get; set; }

       
    }
}
