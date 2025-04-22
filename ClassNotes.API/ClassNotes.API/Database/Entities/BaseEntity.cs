using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Database.Entities
{
    public class BaseEntity
    {

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [StringLength(450)]
        [Column("created_by")]
        public string CreatedBy { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        [StringLength(450)]
        [Column("updated_by")]
        public string UpdatedBy { get; set; }

        [Column("updated_date")]
        public DateTime UpdatedDate { get; set; }

    }
}
