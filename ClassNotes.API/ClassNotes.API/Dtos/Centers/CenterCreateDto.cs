using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ClassNotes.API.Dtos.Centers
{
	public class CenterCreateDto
	{
      
        [Display(Name = "Nombre")]
        [Required(ErrorMessage ="El {0} es requerido.")]
        [StringLength(75, ErrorMessage ="El {0} debe tener menos de {1}.")]
        public string Name { get; set; }

        [Display(Name = "Abreviatura")]
        [StringLength(5,MinimumLength = 1, ErrorMessage ="La {0} debe tener menos de {1} y al menos de {2}.")]
        public string Abbreviation { get; set; }


        [Display(Name = "Logo")]
        [StringLength(250,MinimumLength = 1, ErrorMessage = "El {0} debe tener menos de {1} caracteres y al menos de {2}.")]
        public string Logo { get; set; }


    }
}
