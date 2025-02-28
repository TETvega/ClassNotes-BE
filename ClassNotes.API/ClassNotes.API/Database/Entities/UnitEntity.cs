using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{
	[Table("units", Schema = "dbo")]
	public class UnitEntity : BaseEntity
	{
		[Required]
		[Column("unit_number")]
		public int UnitNumber { get; set; }

		[Required]
		[Column("max_score")]
		public float MaxScore { get; set; }

		[Required]
		[Column("course_id")]
		public Guid CourseId { get; set; }

		[ForeignKey(nameof(CourseId))]
		public virtual CourseEntity Course { get; set; }

		public virtual ICollection<ActivityEntity> Activities { get; set; }

		public virtual UserEntity CreatedByUser { get; set; }
		public virtual UserEntity UpdatedByUser { get; set; }
	}
}
