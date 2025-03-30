namespace ClassNotes.API.Dtos.EmailsAttendace
{
    //DD: DTO Para la creacion de pedido del correo para asistencia 
    public class EmailAttendanceRequestDto
    {
        public string ProfesorId { get; set; }
        public Guid CentroId { get; set; }
        public Guid ClaseId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RangoValidacionMetros { get; set; }
        public int TiempoExpiracionOTPMinutos { get; set; }

        public bool EnvioAutomatico { get; set; }
        public string HoraEnvio {  get; set; }
        public List<DayOfWeek> DiasEnvio {  get; set; }

        public TimeSpan HoraEnvioTimeSpan => TimeSpan.Parse(HoraEnvio);
    }
}
