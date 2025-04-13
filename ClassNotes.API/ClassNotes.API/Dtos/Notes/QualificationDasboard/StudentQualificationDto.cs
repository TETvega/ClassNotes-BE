namespace ClassNotes.API.Dtos.Notes.QualificationDasboard
{
    public class StudentQualificationDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }

        public List<StudentUnitNote> StudentUnits { get; set; }

        public float GlobalAverage { get; set; }

        public string StateNote { get; set; }
    }
}
