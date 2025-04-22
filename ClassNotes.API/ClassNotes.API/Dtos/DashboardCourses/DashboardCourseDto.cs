namespace ClassNotes.API.Dtos.DashboardCourses
{
    // --------------------- CP --------------------- //
    public class DashboardCourseDto
    {
        public int StudentsCount { get; set; } // Contador de estudiantes
        public float ScoreEvaluated { get; set; } // Contador de los puntos que han sido evaluados
        public int PendingActivitiesCount { get; set; } // Contador de las actividades pendientes del curso

        public int PendingNotesRemenbers { get; set; }

        public float MaxScoreEvaluated { get; set; }

        public List<DashboardCourseActivityDto> Activities { get; set; }  // Lista de actividades del curso
        public List<DashboardCourseStudentDto> Students { get; set; } // Lista de estudiantes del curso
    }
}