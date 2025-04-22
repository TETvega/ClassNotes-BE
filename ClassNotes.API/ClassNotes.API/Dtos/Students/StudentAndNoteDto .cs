namespace ClassNotes.API.Dtos.Students
{
	public class StudentAndNoteDto
	{
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public float Score { get; set; }
        public string FeedBack { get; set; }
    }
}
