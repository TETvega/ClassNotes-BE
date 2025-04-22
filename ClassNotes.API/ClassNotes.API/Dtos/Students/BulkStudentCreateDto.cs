namespace ClassNotes.API.Dtos.Students
{
    public class BulkStudentCreateDto
    {
        public bool StrictMode { get; set; }
        public Guid CourseId { get; set; }
        public List<StudentCreateDto> Students { get; set; }
    }
}
