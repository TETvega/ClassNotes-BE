using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("course_notes", Schema = "dbo")]
    public class CourseNoteEntity : BaseEntity
    {
        [Required]
        [StringLength(50)]
        [Column("title")]
        public string Title { get; set; }


        [Required]
        [StringLength(1000)]
        [Column("content")]
        public string Content { get; set; }


        [Required]
        [Column("registration_date")]
        public DateTime RegistrationDate { get; set; }


        [Required]
        [Column("use_date")]
        public DateTime UseDate { get; set; }

        [Required]
        [Column("course_id")]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseEntity Course { get; set; }


        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
