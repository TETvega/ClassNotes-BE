namespace ClassNotes.API.Dtos.CourseFilter
{
    // DTO que representa los filtros disponibles para buscar cursos
    public class CoursesFilterDto
    {
        public string ClassTypes { get; set; } = "ALL"; // Tipo de clase que se desea filtrar ("all", "active", "inactive")
        public List<Guid> Centers { get; set; }= new(); //Lista de Ids de Centros
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = "";
    }
}
