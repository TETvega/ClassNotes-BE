using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.CourseNotes
{
	public class StudentActivityNoteDto
	{
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ActivityId { get; set; }
        public float Note { get; set; }
        public string Feedback { get; set; }
        public bool IsExtra { get; set; }
    }
}
