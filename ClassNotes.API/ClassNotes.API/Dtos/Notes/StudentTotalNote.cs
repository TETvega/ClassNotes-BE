using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.CourseNotes
{
	public class StudentTotalNoteDto
	{
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public float FinalNote { get; set; }
        public float AveragedNote { get; set; }
    }
}
