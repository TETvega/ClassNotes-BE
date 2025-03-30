using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("students", Schema = "dbo")]
    public class StudentEntity : BaseEntity
    {
        [Required]
        [StringLength(450)]
        [Column("teacher_id")]
        public string TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual UserEntity Teacher { get; set; }

        //(Ken)
        //Deje una longitud minima de 2 por si acaso, en ambas propiedades.
        [Required]
        [StringLength(75, MinimumLength = 2)]
        [Column("first_name")]
        public string FirstName { get; set; }


        [StringLength(70, MinimumLength = 2)]
        [Column("last_name")]
        public string LastName { get; set; }

        [Required]
        [StringLength(320)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; }

        public virtual ICollection<StudentActivityNoteEntity> Activities { get; set; }
        public virtual ICollection<AttendanceEntity> Attendances { get; set; }
        public virtual ICollection<StudentCourseEntity> Courses { get; set; }
        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
