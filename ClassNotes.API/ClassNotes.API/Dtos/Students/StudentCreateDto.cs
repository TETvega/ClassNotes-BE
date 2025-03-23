using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Students
{
	public class StudentCreateDto
	{
        [Display(Name = "ID del Profesor")]
        [Required(ErrorMessage = "El ID del profesor es requerido.")]
        public string TeacherId { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre del estudiante es requerido.")]
        [MinLength(2, ErrorMessage = "El {0} debe tener al menos {1} caracteres.")]
        public string FirstName { get; set; }

        [Display(Name = "Apellido")]
        [Required(ErrorMessage = "El apellido del estudiante es requerido.")]
        [MinLength(2, ErrorMessage = "El {0} debe tener al menos {1} caracteres.")]
        public string LastName { get; set; }

        [Display(Name = "Correo Electrónico")]
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; }

        // EG -> Id del curso
        [Display(Name = "id del curso")]
        [Required(ErrorMessage = "El {0} es requerido.")]
        public Guid CourseId { get; set; }
    }
}
