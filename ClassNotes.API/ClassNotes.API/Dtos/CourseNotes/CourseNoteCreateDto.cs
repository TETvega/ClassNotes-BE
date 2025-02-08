using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.CourseNotes
{
	public class CourseNoteCreateDto
	{
        [Required]
        [StringLength(50, ErrorMessage = "El título no puede tener más de 50 caracteres.")]
        public string Title { get; set; }
        [Required]
        [StringLength(1000, ErrorMessage = "El contenido no puede tener más de 250 caracteres.")]
        public string Content { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime UseDate { get; set; }
        [Required]
        public Guid CourseId { get; set; }
    }
}
