using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Dtos.Activities
{
	// --------------------- CP --------------------- //
	public class ActivityCreateDto
	{
		// Nombre
		[Display(Name = "nombre")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[StringLength(50, ErrorMessage = "El {0} debe tener menos de {1} caracteres.")]
		public string Name { get; set; }

        [Required]
        [Display (Name="es extra")]
        public bool IsExtra { get; set; }

        // Puntuación máxima
        [Display(Name = "puntuación máxima")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		[Range(0.01,100, ErrorMessage = "La {0} debe estar entre {1} y {2}")] //(Ken) Para validar que no haya notas menores a 0, ni mayores a 100
		public float MaxScore { get; set; }	

		// Fecha de calificación sera la fecha en la que se piensa calificar la actividad
		[Display(Name = "fecha de calificación")]
		[Required(ErrorMessage = "La {0} es requerida.")]
		public DateTime QualificationDate { get; set; }

        [Required]
        [Display(Name= "Id del tag de la actividad")]
        public Guid TagActivityId { get; set; }

        // Id del curso
        //(ken)
        //Es posible que se necesite incluir el id de el curso, auqnue el propio unit ya yo tenga, dependiendo del frontend...
        [Display(Name = "id de la unidad o parcial")]
		[Required(ErrorMessage = "El {0} es requerido.")]
		public Guid UnitId { get; set; }	
	}
}
