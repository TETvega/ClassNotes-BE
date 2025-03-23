using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("students_units", Schema = "dbo")]
    public class StudentUnitEntity : BaseEntity
    {
        [Required]
        [Column("unit_id")]
        public Guid UnitId { get; set; }
        [ForeignKey(nameof(UnitId))]
        public virtual UnitEntity Unit { get; set; }


        [Required]
        [Column("student_course_id")]
        public Guid StudentCourseId { get; set; }
        [ForeignKey(nameof(StudentCourseId))]
        public virtual StudentCourseEntity StudentCourse { get; set; }

        [Required]
        [Column("unit_number")]
        public int UnitNumber { get; set; }

        [Required]
        [Column("unit_note")]
        [Range(0,100)]
        public float UnitNote { get; set; }



        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
