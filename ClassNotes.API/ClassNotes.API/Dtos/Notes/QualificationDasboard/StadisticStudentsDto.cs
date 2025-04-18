namespace ClassNotes.API.Dtos.Notes.QualificationDasboard
{
    public class StadisticStudentsDto
    {
        public float OverallAvarage { get; set; }
        public float ApprovalRating { get; set; }

        public string ScoreTypeCourse { get; set; }

        public UnitStatus BestUnit { get; set; }
        public UnitStatus WorstUnit { get; set; }

        public GraficResultDTO GraficResult { get; set; }
    }
}
