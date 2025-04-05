namespace ClassNotes.API.Dtos.Allcourses
{
    // DTO que representa los filtros disponibles para buscar cursos
    public class CoursesFilterDto
    {
        public string ClassTypes { get; set; }  // Tipo de clase que se desea filtrar ("all", "active", "inactive")
        public List<Guid> Centers { get; set; } //Lista de Ids de Centros
        public int Page { get; set; }
        public int PageSize { get; set; } = 1;
        public string SearchTerm { get; set; } = "";
    }
}
