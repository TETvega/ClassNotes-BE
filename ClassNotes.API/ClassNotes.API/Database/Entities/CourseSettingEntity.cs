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
        [StringLength(15)]
        [Column("score_type")]
        public string ScoreType { get; set; }

        //(Ken) 
        //Limito las unidades de uno a 7 para que tenga sentido por los dias a la semana.
        [Range(1, 7)]
        [Column("unit")]
        public int Unit { get; set; }


        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; }


        [Column("end_date")]
        public DateTime EndDate { get; set; }


        //(ken)
        //No se puede limitar digitos despues del decimal con DataNotations
        //Asi que lo tienen que hacer en el servicio o frontend...
        [Required]
        [Range(0, 100)]
        [Column("minimum_grade")]
        public float MinimumGrade { get; set; }


        [Required]
        [Column("minimum_attendance_time")]
        [Range(5, 59)]
        [DefaultValue(10)]
        public int MinimumAttendanceTime { get; set; }

        public virtual CourseEntity Course { get; set; }
        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
