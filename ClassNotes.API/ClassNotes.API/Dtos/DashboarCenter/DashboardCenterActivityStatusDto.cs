namespace ClassNotes.API.Dtos.DashboarCenter
{
    public class DashboardCenterActivityStatusDto
    {
        public int Total { get; set; }                // Total de actividades
        public int CompletedCount { get; set; }       // Actividades completadas
        public int PendingCount { get; set; }         // Actividades aún no realizadas
        public string NextActivity { get; set; }      // Nombre de la próxima actividad
        public DateTime NextActivityDate { get; set; } // Fecha de la próxima actividad
        public DateTime? LastExpiredDate { get; set; } // Última fecha de actividad expirada
    }
}
