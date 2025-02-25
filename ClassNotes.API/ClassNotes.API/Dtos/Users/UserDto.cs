namespace ClassNotes.API.Dtos.Users
{
	public class UserDto
	{
		public string Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
        public Guid? DefaultCourseSettingId { get; set; }
    }
}
