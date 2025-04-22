using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.AttendacesRealTime
{
    public class AttendanceTypeDto
    {
        [Display(Name = "Envio por email")]
        [Required(ErrorMessage = "El {0} es requerido")]
        public bool Email { get; set; }
        [Display(Name = "Envio po Qr")]
        [Required(ErrorMessage = "El {0} es requerido")]
        public bool Qr { get; set; }
    }
}
