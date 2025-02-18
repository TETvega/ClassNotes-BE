using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("centers", Schema = "dbo")]
    public class CenterEntity : BaseEntity
    {
        [Required]
        [StringLength(75)]
        [Column("name")]
        public string Name { get; set; }


        [StringLength(10)]
        [Column("abbreviation")]
        public string Abbreviation { get; set; }


        [StringLength(250)]
        [Column("logo")]
        public string Logo { get; set; }


        [Required]
        [Column("is_archived")]
        public bool IsArchived { get; set; }


        [StringLength(450)]
        [Column("teacher_id")]
        public string TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual UserEntity Teacher { get; set; }


        public virtual ICollection<CourseEntity> Courses { get; set; }
        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
