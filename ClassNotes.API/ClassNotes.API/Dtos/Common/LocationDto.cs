using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Common
{
    public class LocationDto
    {

        [Display(Name = "Longitud")]
        [Required(ErrorMessage = "la {0} es requerida")]
        public double X { get; set; } // Longitud
        [Display(Name = "Latitud")]
        [Required(ErrorMessage = "la {0} es requerida")]
        public double Y { get; set; } // Latitud
    }
}
