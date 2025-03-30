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

        [Required]
        [Column("start_time")]
		public TimeSpan StartTime { get; set; } // Se cambio a TimeSpan para poder enviar la hora en formato hora:minutos

        [Column("finish_time")]
		public TimeSpan? FinishTime { get; set; }

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


        // [Required] Ya no deberia de ser requerida porque cuando no se pasa un id de configuración se crea una config por defecto
        [Column("setting_id")]
        public Guid? SettingId { get; set; }
        [ForeignKey(nameof(SettingId))]

        public virtual CourseSettingEntity CourseSetting { get; set;}


        public virtual ICollection<ActivityEntity> Activities { get; set; }
        public virtual ICollection<AttendanceEntity> Attendances { get; set; }
        //DD: se Agreco esta linea para poder navegar student course 
        public virtual ICollection<StudentCourseEntity> StudentsCourses { get; set; }
        // Cambio

        public virtual ICollection<UnitEntity> Units { get; set; }
        public virtual ICollection<StudentCourseEntity> Students { get; set; }
        public virtual ICollection<CourseNoteEntity> CourseNotes { get; set; }

        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }

    }
}
