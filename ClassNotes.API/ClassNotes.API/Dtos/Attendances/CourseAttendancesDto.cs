using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Dtos.Attendances
{
	public class CourseAttendancesDto
	{
		public int AttendanceTakenDays { get; set; }
		public double AttendanceRating { get; set; }
		public double AbsenceRating { get; set; }
	}
}
