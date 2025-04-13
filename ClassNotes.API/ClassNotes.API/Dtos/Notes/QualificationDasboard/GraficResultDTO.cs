namespace ClassNotes.API.Dtos.Notes.QualificationDasboard
{
    public class GraficResultDTO
    {
        public int BigTotal { get; set; }

        public int ExcellentTotal { get; set;}

        public int GoodTotal { get; set; }
        public int StablishTotal { get; set; }
        public int LowTotal { get; set; }

        public int FailedTotal { get; set; }
    }
}
