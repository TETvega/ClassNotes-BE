using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;
using ClassNotes.API.Dtos.QR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/QR")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class QRController : ControllerBase
{
    private readonly QRService _qrService;
    private readonly ILogger<QRController> _logger;

    public QRController(QRService qrService, ILogger<QRController> logger)
    {
        _qrService = qrService;
        _logger = logger;
    }

    // Endpoint para generar el código QR
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQR([FromBody] QRGenerationRequestDto request)
    {
        try
        {
            // Método para obtener el ID del profesor de varias formas posibles
            string teacherId = GetTeacherId();

            if (string.IsNullOrEmpty(teacherId))
            {
                _logger.LogWarning("Intento de generar QR sin ID de profesor válido");
                return Unauthorized(new { Message = "Usuario no autenticado correctamente. No se pudo identificar al profesor." });
            }

            _logger.LogInformation($"Generando QR para profesor ID: {teacherId}, clase ID: {request.ClaseId}");
            var result = await _qrService.GenerarQR(request, teacherId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar código QR");
            return StatusCode(500, new { Message = "Ocurrió un error inesperado al generar el código QR.", Error = ex.Message });
        }
    }

    // Endpoint para validar el código QR
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateQR([FromBody] QRValidationRequestDto request)
    {
        try
        {
            _logger.LogInformation($"Validando QR para estudiante con correo: {request.EstudianteCorreo}");
            var result = await _qrService.ValidateQR(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar código QR");
            return StatusCode(500, new { Message = "Ocurrió un error inesperado al validar el código QR.", Error = ex.Message });
        }
    }

    // Método auxiliar para obtener el ID del profesor de diferentes maneras posibles
    private string GetTeacherId()
    {
        // Registra todas las claims disponibles para depuración
        var claimsInfo = string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"));
        _logger.LogDebug($"Claims disponibles: {claimsInfo}");

        // Intenta diferentes tipos de claims comunes
        string teacherId = null;

        // Opción: Claim de ID usuario genérica
        teacherId = User.FindFirst("userId")?.Value ?? User.FindFirst("uid")?.Value;
        if (!string.IsNullOrEmpty(teacherId)) return teacherId;
        return teacherId;
    }
}