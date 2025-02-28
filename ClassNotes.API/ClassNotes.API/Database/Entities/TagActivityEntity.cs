using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
    [Table("tags_activities", Schema = "dbo")]
    public class TagActivityEntity : BaseEntity
    {
        [Required]
        [StringLength(20)]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [StringLength(7)]
        [Column("color_hex")] 
        public string ColorHex { get; set; }

        [Required]
        [StringLength(5)]
        [Column ("icon")]
        public string Icon { get; set; }


        [Required]
        [StringLength(450)]
        [Column("teacher_id")]
        public string TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual UserEntity Teacher { get; set; }

        public virtual UserEntity CreatedByUser { get; set; }
        public virtual UserEntity UpdatedByUser { get; set; }
    }
}
