using System.ComponentModel.DataAnnotations;

namespace ClassNotes.API.Dtos.Activities
{
	public class ActivityDto
    {
        public Guid Id { get; set; }
        // Nombre
        public string Name { get; set; }


        public bool IsExtra { get; set; }

        // Puntuación máxima
        public float MaxScore { get; set; }

       
        public DateTime QualificationDate { get; set; }

        public Guid TagActivityId { get; set; }

        // Id del curso
        public Guid UnitId { get; set; }
    }
}
