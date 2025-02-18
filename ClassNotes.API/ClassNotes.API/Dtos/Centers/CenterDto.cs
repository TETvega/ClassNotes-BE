using ClassNotes.API.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Centers
{
	public class CenterDto
	{
        public string Name { get; set; }


        public string Abbreviation { get; set; }


        public string Logo { get; set; }


        public bool IsArchived { get; set; }


        public string TeacherId { get; set; }


    }
}
