namespace ClassNotes.API.Dtos.Students
{
	public class StudentDto
	{
        public Guid Id { get; set; } // Debe de mostrar el id del estudiante, no del docente
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

    }
}
