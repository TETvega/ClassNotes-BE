using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ClassNotes.API.Dtos.QR;
using ClassNotes.API.Services;
using Microsoft.AspNetCore.Authorization;
using ClassNotes.API.Constants;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class QRController : ControllerBase
{
    private readonly QRService _qrService;

    public QRController(QRService qrService)
    {
        _qrService = qrService;
    }

    //DD: Endpoint para generar el código QR
    [HttpPost("generate")]
    [Authorize(Roles = $"{RolesConstant.USER}")]
    public async Task<IActionResult> GenerateQR([FromBody] QRGenerationRequestDto request)
    {
        try
        {
            var result = await _qrService.GenerateQR(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Ocurrió un error inesperado al generar el código QR.", Error = ex.Message });
        }
    }

    //DD: Endpoint para validar el código QR
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateQR([FromBody] QRValidationRequestDto request)
    {
        try
        {
            var result = await _qrService.ValidateQR(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Ocurrió un error inesperado al validar el código QR.", Error = ex.Message });
        }
    }
}