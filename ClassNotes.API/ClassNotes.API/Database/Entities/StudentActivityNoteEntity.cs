using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("students_activities_notes", Schema = "dbo")]
    public class StudentActivityNoteEntity : BaseEntity
    {
        [Required]
        [Column("student_id")]
        public Guid StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public virtual StudentEntity Student { get; set; }


        [Required]
        [Column("activity_id")]
        public Guid ActivityId { get; set; }
        [ForeignKey(nameof(ActivityId))]
        public virtual ActivityEntity Activity { get; set; }


        //(Ken)
        //Rango para calificacion = 1 a 100...
        //No se puede limitar digitos despues del decimal con DataNotations
        //Asi que lo tienen que hacer en el servicio o frontend...
        [Required]
        [Column("note")]
        [Range(0, 100)]
        public float Note { get; set; }


        [Required]
        [StringLength(250)]
        [Column("feedback")]
        public string Feedback { get; set; }


        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
