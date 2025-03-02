namespace ClassNotes.API.Dtos.Emails
{
	public class EmailGradeDto
	{
		public Guid CourseId { get; set; }
		public Guid StudentId { get; set; }
		public string Content { get; set; }
	}
}
