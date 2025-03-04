using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Attendances
{
	public class AttendanceCreateDto 
	{
        [Required]
        public string Attended { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; }

        [Required]
        public Guid CourseId { get; set; }
        [Required]
        public Guid StudentId { get; set; }
    }
}
