using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.TagsActivities
{
	public class TagActivityDto
	{
		public string Name { get; set; }
		public string ColorHex { get; set; }
		public string Icon { get; set; }
		public string TeacherId { get; set; }
	}
}
