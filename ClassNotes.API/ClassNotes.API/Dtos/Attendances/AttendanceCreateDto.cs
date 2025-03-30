using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Attendances
{
	public class AttendanceCreateDto 
	{
        [Required]
        public bool Attended { get; set; }

        public string Status { get; set; }
        public DateTime RegistrationDate { get; set; }

        [Required]
        public Guid CourseId { get; set; }
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public string TeacherId { get; set; }
    }
}
