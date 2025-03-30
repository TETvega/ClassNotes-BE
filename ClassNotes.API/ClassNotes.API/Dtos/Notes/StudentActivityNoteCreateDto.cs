using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Dtos.CourseNotes
{
	public class StudentActivityNoteCreateDto
	{
        [Display(Name="id de estudiante")]
        [Required(ErrorMessage = "El {0} es requerido.")]
        public Guid StudentId { get; set; }



        [Display(Name = "Nota")]
        [Required(ErrorMessage = "La {0} es requerida.")]
        [Range(0, 100, ErrorMessage = "La {0} debe estar entre {1} y {2}")]
        public float Note { get; set; }

        [Display(Name = "Comentario")]
        [Required(ErrorMessage = "El {0} es requerido.")]
        [StringLength(250, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
        public string Feedback { get; set; }

        public bool IsExtra { get; set; }

    }
}
