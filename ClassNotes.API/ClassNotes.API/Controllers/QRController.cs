using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Linq;
using System.Threading.Tasks;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.QR;
using ClassNotes.API.Services;
using ClassNotes.API.Services.Distance; 

//DD: Controlador para la generacion del codigo Qr
[ApiController]
[Route("api/[controller]")]
public class QRController : ControllerBase
{
    private readonly ClassNotesContext _context;
    private readonly DistanceService _distanceService; 

    public QRController(ClassNotesContext context, DistanceService distanceService)
    {
        _context = context;
        _distanceService = distanceService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQR([FromBody] QRGenerationRequestDto request)
    {
        // Validar si el profesor existe
        var profesor = await _context.Users.FindAsync(request.ProfesorId);
        if (profesor == null) return BadRequest("El profesor no existe.");

        // Validar si el centro existe
        var centro = await _context.Centers.FindAsync(request.CentroId);
        if (centro == null) return BadRequest("El centro no existe.");

        // Validar si la clase existe y está asociada con el profesor y el centro
        var clase = await _context.Courses
            .Where(c => c.Id == request.ClaseId && c.TeacherId == request.ProfesorId && c.CenterId == request.CentroId)
            .FirstOrDefaultAsync();
        if (clase == null) return BadRequest("La clase no está asociada con el profesor o el centro.");

        // Generar la URL de validación con datos embebidos en el QR
        var timestamp = DateTime.UtcNow.ToString("o");
        var qrContent = $"{request.ProfesorId}|{request.CentroId}|{request.ClaseId}|{request.Latitud}|{request.Longitud}|{timestamp}";

        string validationUrl = $"https://localhost:7047/api/QR/validate?qrContent={Uri.EscapeDataString(qrContent)}";

        // Generar código QR con la URL
        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        {
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(validationUrl, QRCodeGenerator.ECCLevel.Q);
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                var qrCodeBase64 = Convert.ToBase64String(qrCodeImage);
                return Ok(new { QRCode = qrCodeBase64, ValidationUrl = validationUrl });
            }
        }
    }

    [HttpPost("validate")]
    public IActionResult ValidateQR([FromBody] QRValidationRequestDto request)
    {
        var qrParts = request.QRContent.Split('|');
        if (qrParts.Length < 6) return BadRequest("Código QR inválido.");

        string profesorId = qrParts[0];
        Guid centroId = Guid.Parse(qrParts[1]);
        Guid claseId = Guid.Parse(qrParts[2]);
        double latitud = double.Parse(qrParts[3]);
        double longitud = double.Parse(qrParts[4]);
        DateTime qrTimestamp = DateTime.Parse(qrParts[5]);

        // Validar si el QR ha expirado (5 minutos)
        if (DateTime.UtcNow > qrTimestamp.AddMinutes(5))
        {
            return BadRequest("El código QR ha expirado.");
        }

        // Validar si el estudiante está dentro del rango de 15 metros usando el servicio
        double distancia = _distanceService.CalcularDistancia(latitud, longitud, request.EstudianteLatitud, request.EstudianteLongitud);
        if (distancia > 15)
        {
            return BadRequest("Estás fuera del rango permitido para confirmar la asistencia.");
        }

        return Ok(new { Message = "Asistencia confirmada.", Estudiante = request.EstudianteNombre, Correo = request.EstudianteCorreo });
    }
}