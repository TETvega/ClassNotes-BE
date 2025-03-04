using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("attendances", Schema = "dbo")]
    public class AttendanceEntity : BaseEntity
    {
        [Required]
        [Column("attended")]
        public string Attended { get; set; }


        [Required]
        [Column("registration_date")]
        public DateTime RegistrationDate { get; set; }


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


        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
