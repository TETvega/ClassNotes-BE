using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Students
{
	public class StudentCreateDto
	{
 
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
 
    }
}
