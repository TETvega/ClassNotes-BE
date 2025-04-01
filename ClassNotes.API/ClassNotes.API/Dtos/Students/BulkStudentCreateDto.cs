namespace ClassNotes.API.Dtos.Students
{
    public class BulkStudentCreateDto
    {
        public string TeacherId { get; set; }
        public Guid CourseId { get; set; }
        public List<StudentCreateDto> Students { get; set; }
    }
}
