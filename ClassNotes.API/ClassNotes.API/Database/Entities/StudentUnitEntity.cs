using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("students_units", Schema = "dbo")]
    public class StudentUnitEntity : BaseEntity
    {
        [Required]
        [Column("unit_id")]
        public Guid UnitId { get; set; }
        [ForeignKey(nameof(UnitId))]
        public virtual UnitEntity Unit { get; set; }


        [Required]
        [Column("student_id")]
        public Guid StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public virtual StudentEntity Student { get; set; }


        [Required]
        [Column("unit_note")]
        [Range(0,100)]
        public float UnitNote { get; set; }



        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
