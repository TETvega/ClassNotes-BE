namespace ClassNotes.API.Dtos.QR
{
    public class QRGenerationRequestDto
    {
        public string ProfesorId { get; set; }
        public Guid CentroId { get; set; }
        public Guid ClaseId { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public int TiempoLimiteMinutos { get; set; } 
        public int RangoValidacionMetros { get; set; } 
    }
}
