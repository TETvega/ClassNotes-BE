namespace ClassNotes.API.Dtos.Attendances.Student
{
    public class StudentAttendancesDto
    {
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
        public string  StudentEmail { get; set; }
        public double AttendanceCount { get; set; }
        public double AttendanceRate { get; set; }
        public double AbsenceCount { get; set; }
        public double AbsenceRate { get; set; }
        public bool IsActive { get; set; }
    }
}
