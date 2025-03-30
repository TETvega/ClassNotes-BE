using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel;

namespace ClassNotes.API.Database.Entities
{
    [Table("courses_settings", Schema = "dbo")]
    public class CourseSettingEntity : BaseEntity
    {
        [Required]
        [StringLength(25)]
        [Column("name")] // Campo agregado para diferenciar las diferentes configuraciones de curso
        public string Name { get; set; }

        [Required]
        [StringLength(15)]
        [Column("score_type")]
        public string ScoreType { get; set; }

        // Se elimino la parte de unidades debido a refactorización

        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; } // Fecha de inicio del periodo


        [Column("end_date")]
        public DateTime? EndDate { get; set; } // Fecha de fin de periodo


        // Kenn
        //No se puede limitar digitos despues del decimal con DataNotations
        //Hay que limitarlo en el Servicio a [2] decimales despues del Punto

        [Required]
        [Range(0, 100)]
        [Column("minimum_grade")]
        public float MinimumGrade { get; set; }

        [Required]
        [Range(0, 100)]
        [Column("maximum_grade")]
        public float MaximumGrade { get; set; }

        [Required]
        [Column("minimum_attendance_time")]
        [Range(5, 59)]
        [DefaultValue(10)]
        public int MinimumAttendanceTime { get; set; }

        [Required]
        [Column("is_original")]
        public bool IsOriginal { get; set; } // Se agrego este nuevo campo ya que se necesitaba determinar cuales configuraciones son originales y cuales solo son replicas de una original (copias)

        public virtual CourseEntity Course { get; set; } // Se modifico para la relación de uno a uno con cursos
        public virtual ICollection<UserEntity> Teachers { get; set; } = new List<UserEntity>();

        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
