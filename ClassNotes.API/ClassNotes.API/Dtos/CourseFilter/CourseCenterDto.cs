using System.Diagnostics;

namespace ClassNotes.API.Dtos.CourseFilter
{
    // DTO que representa un curso junto con informacion del centro 
    public class CourseCenterDto
    {
        public Guid Id { get; set; } // Id del curso
        public string Name { get; set; } //Nombre del curso
        public string Code { get; set; } // Codigo del curso
        public string AbbCenter { get; set; } //Abreviatura de Centro
        public Guid CenterId { get; set; } // Id del curso
        public string CenterName { get; set; } //Nombre del curso
        public int ActiveStudents { get; set; } //Cantidad de alumnos en el curso
        public ActivitiesDto Activities { get; set; } //Resumen de actividades del curso 
        public bool IsActive { get; set; } // ver si el curso esta activo o inactivo
    }
}
