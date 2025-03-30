namespace ClassNotes.API.Dtos.Students
{
    public class StudentResultDto
    {
        // devuleve listado de los estudiantes que fueron creados exitosamente 
        public List<StudentDto> SuccessfulStudents { get; set; }

        // Devuleve un listado de los estudiantes que tienen datos duplicados
        public List<StudentDto> DuplicateStudents { get; set; }

        // Devulve listado de los estudiantes que se les modifico el correo
        public List<StudentDto> ModifiedEmailStudents { get; set; }

    }
}
