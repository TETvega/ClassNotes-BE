using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Attendances
{
	public class AttendanceDto
	{
        public Guid Id { get; set; }
        public string Attended { get; set; }
        public DateTime RegistrationDate { get; set; }

        public Guid CourseId { get; set; }

        public Guid StudentId { get; set; }
     
    }
}
