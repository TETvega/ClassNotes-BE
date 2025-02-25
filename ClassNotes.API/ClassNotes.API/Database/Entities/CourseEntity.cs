using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("courses", Schema = "dbo")]
    public class CourseEntity : BaseEntity
    {
        [Required]
        [StringLength(50)]
        [Column("name")]
        public string Name { get; set; }

        [StringLength(4)]
        [Column("section")]
        public string Section { get; set; }

        [StringLength(15)]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; }

        [Required]
        [Column("center_id")]
        public Guid CenterId { get; set; }
        [ForeignKey(nameof(CenterId))]

        public virtual CenterEntity Center { get; set; }


        [Required]
        [Column("setting_id")]
        public Guid SettingId { get; set; }
        [ForeignKey(nameof(SettingId))]

        public virtual CourseSettingEntity CourseSetting { get; set;}


        public virtual ICollection<ActivityEntity> Activities { get; set; }
        public virtual ICollection<AttendanceEntity> Attendances { get; set; }
        public virtual ICollection<StudentCourseEntity> Students { get; set; }
        public virtual ICollection<CourseNoteEntity> CourseNotes { get; set; }

        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }

    }
}
