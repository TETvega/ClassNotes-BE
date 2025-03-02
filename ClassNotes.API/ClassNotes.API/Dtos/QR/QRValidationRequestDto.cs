namespace ClassNotes.API.Dtos.QR
{
    //DD: Dto para valiadcion del Qr
    public class QRValidationRequestDto
    {
        public string QRContent { get; set; }
        public string EstudianteNombre{ get;set; }
        public string EstudianteCorreo { get; set; }
        public double EstudianteLatitud { get; set; }
        public double EstudianteLongitud { get; set; }
    }
}
