using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Dtos.Courses
{

	public class UnitDto
	{
        public Guid Id { get; set; }
        public int UnitNumber { get; set; }
        public float? MaxScore { get; set; }
        public Guid CourseId { get; set; }

    }
}
