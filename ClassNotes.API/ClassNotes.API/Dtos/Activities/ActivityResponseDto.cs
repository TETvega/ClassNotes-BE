namespace ClassNotes.API.Dtos.Activities
{
    public class ActivityResponseDto
    {
        public Guid ActivityId { get; set; }
        public string ActivityName { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime QualificationDate { get; set; }
        public float MaxScore { get; set; }
        public bool IsExtra { get; set; }
        public Guid UnitId { get; set; }
        public int UnitNumber { get; set; }

        public Guid  TagActivityId { get; set; }

    }
}
