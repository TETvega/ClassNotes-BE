using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Dtos.CourseNotes
{
	public class StudentUnitNoteDto
	{

        public Guid UnitId { get; set; }
        public int UnitNumber { get; set; }
        public float UnitNote { get; set; }
    }
}
