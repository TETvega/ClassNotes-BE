using ClassNotes.API.Dtos.Common;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class AttendanceRequestDto
    {
        
        [Display(Name ="stricMode ")]
        [Required(ErrorMessage = "El {0} es requerido")]
        public bool StrictMode { get; set; }

        [Display(Name = "Couerse Id")]
        [Required(ErrorMessage = "El {0} es requerido")]
        public Guid CourseId { get; set; }
        [Display(Name = "Punto de referencia")]
        [Required(ErrorMessage = "El {0} es requerido")]
        public bool HomePlace { get; set; } // si este esta en falso se usara la de CourseSettings

        public LocationDto NewGeolocation { get; set; } = null; // Puede ser nulo
        public AttendanceTypeDto AttendanceType { get; set; } // Para saber cual camino tenemos que seguir
    }
}
