namespace ClassNotes.API.Dtos.Attendances
{
	public class StudentAttendancesDto
	{
		public string StudentName { get; set; }
		public double AttendanceCount { get; set; }
		public double AttendanceRate { get; set; }
		public double AbsenceCount { get; set; }
		public double AbsenceRate { get; set; }
		public bool IsActive { get; set; }
	}
}
