namespace ClassNotes.API.Dtos.Activities
{
	// --------------------- CP --------------------- //
	public class ActivityDto
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public int GradingPeriod { get; set; }

		public int MaxScore { get; set; }

		public DateTime QualificationDate { get; set; }

		public Guid CourseId { get; set; }
	}
}
