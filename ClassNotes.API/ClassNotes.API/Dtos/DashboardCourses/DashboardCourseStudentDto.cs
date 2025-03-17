using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassNotes.API.Dtos.DashboardCourses
{
    // --------------------- CP --------------------- //
    public class DashboardCourseStudentDto
    {
        public Guid Id { get; set; } // Id del estudiante
        public string FullName { get; set; } // Nombre completo del estudiante
    }
}