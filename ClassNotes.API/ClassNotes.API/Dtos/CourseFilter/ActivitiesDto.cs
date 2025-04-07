namespace ClassNotes.API.Dtos.CourseFilter
{
    // DTO que representa información resumida sobre las actividades de un curso
    public class ActivitiesDto
    {
        // Total de actividades asociadas al curso
        public int Total { get; set; }

        // Total de actividades que han sido evaluadas 
        public int TotalEvaluated { get; set; }
    }
}
