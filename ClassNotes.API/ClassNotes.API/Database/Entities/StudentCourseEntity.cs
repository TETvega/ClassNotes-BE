using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("students_courses", Schema = "dbo")]
    public class StudentCourseEntity : BaseEntity
    {
        [Required]
        [Column("course_id")]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseEntity Course { get; set; }


        [Required]
        [Column("student_id")]
        public Guid StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public virtual StudentEntity Student { get; set; }


        [Required]
        [Column("final_note")]
        [Range(0,100)]
        public float FinalNote { get; set; }


        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;


        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
