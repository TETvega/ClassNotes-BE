namespace ClassNotes.API.Dtos.Attendances
{
	public class CourseAttendancesStudentDto
	{
		public Guid Id { get; set; }
		public string StudentName { get; set; }
		public string Email { get; set; }
		public double? AttendanceRate { get; set; }
		public bool IsActive { get; set; }
	}
}
