namespace ClassNotes.API.Dtos.QR
{
    //DD: Dto Contenido par ala Generacion del QR 
    public class QRGenerationRequestDto
    {
        public string ProfesorId { get; set; }
        public Guid CentroId { get; set; }
        public Guid ClaseId { get; set; }
        public double Latitud { get; set; } 
        public double Longitud { get; set; }
    }
}
