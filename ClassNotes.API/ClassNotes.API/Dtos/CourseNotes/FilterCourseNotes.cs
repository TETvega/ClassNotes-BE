using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.CourseNotes
{
    public class FilterCourseNotes
    {
        [Required(ErrorMessage = "El ID del curso es obligatorio.")]
        public Guid CourseId { get; set; } // notas del curso buscado
        public string SearchTerm { get; set; } = ""; // palabra de busqueda
        public int Page { get; set; } = 1; // pagina
        public int? PageSize { get; set; } // tama;o de pagina

        [Required(ErrorMessage = "El filtro es obligatorio.")]
        [RegularExpression("^(PENDING|HISTORY)$", ErrorMessage = "El filtro solo puede ser 'PENDING' o 'HISTORY'.")]
        public string Filter { get; set; } = "PENDING"; 


    }
}
