using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("activities", Schema = "dbo")]
    public class ActivityEntity : BaseEntity
    {
        [Required]
        [StringLength(50)]
        [Column("name")]
        public string Name { get; set; }


        [Required]
        [Range(1, 9)]
        [Column("grading_period")]
        public int GradingPeriod { get; set; }


        [Required]
        [Range (0, 100)]
        [Column("max_score")]
        public float MaxScore { get; set; }


        [Required]
        [Column("qualification_date")]
        public DateTime QualificationDate { get; set; }


        [Required]
        [Column("course_id")]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseEntity Course { get; set; }

        public virtual ICollection<StudentActivityNoteEntity> StudentNotes { get; set; }
        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
