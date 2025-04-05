using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.CourseNotes
{
	public class CourseNoteDto
	{
        
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime UseDate { get; set; }
        public Guid CourseId { get; set; }
    }
}
