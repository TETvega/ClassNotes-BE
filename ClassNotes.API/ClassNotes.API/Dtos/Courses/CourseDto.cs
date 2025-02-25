namespace ClassNotes.API.Dtos.Courses
{
    // --------------------- CP --------------------- //
	public class CourseDto
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string Section { get; set; }

		public string Code { get; set; }

		public bool IsActive { get; set; }

		public Guid CenterId { get; set; }

		public Guid SettingId { get; set; }
	}
}
