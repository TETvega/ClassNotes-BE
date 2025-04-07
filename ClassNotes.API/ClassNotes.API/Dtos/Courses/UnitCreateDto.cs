using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Courses
{
	public class UnitCreateDto
	{


        [Display(Name = "número de unidad")]
        [Required(ErrorMessage = "El {0} es requerido.")]
        [Range(1, int.MaxValue, ErrorMessage = "El {0} no puede ser menor a {1}.")] // (Ken) Para que no pongan 0 o negativos...
        public int UnitNumber { get; set; }

        [Display(Name = "puntaje máximo")]
        public float? MaxScore { get; set; }//Para permitir nulos en caso de que sea tipo oro...



    }
}
