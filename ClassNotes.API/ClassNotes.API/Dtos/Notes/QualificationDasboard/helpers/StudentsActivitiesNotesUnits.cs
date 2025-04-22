namespace ClassNotes.API.Dtos.Notes.QualificationDasboard.helpers
{
    public class StudentsActivitiesNotesUnits
    {
        public Guid StudentId { get; set; }

        public Guid ActivityUnitId { get; set; }

        public int UnitNumber { get; set; }
        public float UnitEvaluatedWeigthScore { get; set; }
        public float StudentNote { get; set; }
        public float ActivityMaxScore { get; set; }
    }
}
