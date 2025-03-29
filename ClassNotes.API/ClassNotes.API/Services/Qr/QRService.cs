using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.QR;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Services.Distance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class QRService : BackgroundService
{
    private readonly ILogger<QRService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DistanceService _distanceService;
    private static DateTime _qrExpirationTime;
    private bool _isRunning = false;
    private bool _messageShown = false;
    private int _validationCount = 0;
    private Timer _expirationTimer;
    private static List<string> _macAddressesPermitidas = new List<string>();
    private Guid _currentClassId;

    public QRService(
        ILogger<QRService> logger,
        IServiceScopeFactory scopeFactory,
        DistanceService distanceService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _distanceService = distanceService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de QR iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_isRunning)
                {
                    if (!_messageShown)
                    {
                        _logger.LogInformation("Servicio de QR inactivo. Esperando reactivación...");
                        _messageShown = true;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                _validationCount++;
                _logger.LogInformation($"Validación #{_validationCount} - QR activo");

                if (DateTime.UtcNow > _qrExpirationTime)
                {
                    _logger.LogInformation("QR expirado. Procesando asistencias pendientes...");
                    await ProcesarAsistenciasExpiradas();
                    _isRunning = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el servicio de QR");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcesarAsistenciasExpiradas()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();

        var asistenciasPendientes = await context.Attendances
            .Where(a => a.Attended == false && a.CourseId == _currentClassId)
            .ToListAsync();

        foreach (var asistencia in asistenciasPendientes)
        {
            _logger.LogInformation($"Marcando asistencia {asistencia.Id} como no presente (QR expirado)");
        }

        _macAddressesPermitidas.Clear();
        _logger.LogInformation("Procesamiento de QR expirado completado");
    }

    public async Task<QRGenerationResponseDto> GenerateQR(QRGenerationRequestDto request)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();

        // Validaciones básicas
        if (request.TiempoLimiteMinutos < 1 || request.TiempoLimiteMinutos > 30)
            throw new ArgumentException("Tiempo límite debe ser entre 1 y 30 minutos");

        if (request.RangoValidacionMetros < 15 || request.RangoValidacionMetros > 100)
            throw new ArgumentException("Rango de validación debe ser entre 15 y 100 metros");

        // Validar que el centro pertenece al profesor
        var centroValido = await context.Centers
            .AnyAsync(c => c.Id == request.CentroId && c.TeacherId == request.ProfesorId);

        if (!centroValido)
        {
            throw new ArgumentException("El centro no existe o no pertenece al profesor especificado.");
        }

        // Obtener la clase verificando que pertenece al centro
        var clase = await context.Courses
            .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.CenterId == request.CentroId);

        if (clase == null)
        {
            throw new ArgumentException("La clase no está asociada con el centro especificado.");
        }

        _currentClassId = request.ClaseId;

        // Crear registros de asistencia en espera (Attended = false)
        var estudiantes = await context.StudentsCourses
            .Where(sc => sc.CourseId == request.ClaseId && sc.IsActive)
            .Select(sc => sc.Student)
            .ToListAsync();

        foreach (var estudiante in estudiantes)
        {
            await attendanceService.CreateAttendanceAsync(new AttendanceCreateDto
            {
                Attended = false,
                Status = "En Espera",
                CourseId = request.ClaseId,
                StudentId = estudiante.Id
            });
        }

        // Generar contenido del QR
        var qrContent = $"{request.ProfesorId}|{request.CentroId}|{request.ClaseId}|" +
                       $"{request.Latitud}|{request.Longitud}|{DateTime.UtcNow:o}|" +
                       $"{request.TiempoLimiteMinutos}|{request.RangoValidacionMetros}|" +
                       $"{request.PermitirMultiplesDispositivos}";

        _qrExpirationTime = DateTime.UtcNow.AddMinutes(request.TiempoLimiteMinutos);
        _isRunning = true;
        _messageShown = false;

        // Generar QR
        var validationUrl = $"https://tudominio.com/validate-qr?data={Uri.EscapeDataString(qrContent)}";
        var qrCode = GenerateQRCode(validationUrl);

        return new QRGenerationResponseDto
        {
            QRCode = qrCode,
            ExpirationTime = _qrExpirationTime,
            ValidationUrl = validationUrl
        };
    }

    public async Task<QRValidationResponseDto> ValidateQR(QRValidationRequestDto request)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();
        var distanceService = scope.ServiceProvider.GetRequiredService<DistanceService>();

        try
        {
            // Parsear datos del QR
            var qrData = request.QRContent.Split('|');
            if (qrData.Length != 9) throw new ArgumentException("Formato de QR inválido");

            var profesorId = qrData[0];
            var centroId = Guid.Parse(qrData[1]);
            var claseId = Guid.Parse(qrData[2]);
            var qrLatitud = double.Parse(qrData[3]);
            var qrLongitud = double.Parse(qrData[4]);
            var rangoValidacion = int.Parse(qrData[7]);
            var permitirMultiples = bool.Parse(qrData[8]);

            // Verificar que el centro pertenece al profesor
            var centroValido = await context.Centers
                .AnyAsync(c => c.Id == centroId && c.TeacherId == profesorId);

            if (!centroValido)
            {
                throw new ArgumentException("El centro no existe o no pertenece al profesor especificado.");
            }

            // Verificar que la clase pertenece al centro
            var claseValida = await context.Courses
                .AnyAsync(c => c.Id == claseId && c.CenterId == centroId);

            if (!claseValida)
            {
                throw new ArgumentException("La clase no está asociada con el centro especificado.");
            }

            // Verificar estudiante
            var estudiante = await context.Students
                .FirstOrDefaultAsync(s => s.Email == request.EstudianteCorreo);
            if (estudiante == null) throw new ArgumentException("Estudiante no encontrado");

            // Verificar que el estudiante pertenece a la clase
            var enClase = await context.StudentsCourses
                .AnyAsync(sc => sc.StudentId == estudiante.Id && sc.CourseId == claseId && sc.IsActive);
            if (!enClase) throw new ArgumentException("Estudiante no pertenece a esta clase");

            // Validar ubicación
            var distancia = distanceService.CalcularDistancia(
                qrLatitud, qrLongitud,
                request.EstudianteLatitud, request.EstudianteLongitud);

            if (distancia > rangoValidacion)
                throw new ArgumentException($"Debe estar dentro de {rangoValidacion}m. Distancia actual: {distancia}m");

            // Validar dispositivo (si aplica)
            if (!permitirMultiples && !string.IsNullOrEmpty(request.MacAddress))
            {
                if (_macAddressesPermitidas.Contains(request.MacAddress))
                    throw new ArgumentException("Este dispositivo ya validó asistencia");

                _macAddressesPermitidas.Add(request.MacAddress);
            }

            // Obtener asistencia
            var asistencia = await context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == estudiante.Id && a.CourseId == claseId);

            if (asistencia == null) throw new ArgumentException("Registro de asistencia no encontrado");

            // Si ya está marcado como presente
            if (asistencia.Attended == true)
            {
                return new QRValidationResponseDto
                {
                    Success = false,
                    Message = "Ya has validado tu asistencia",
                    Attendance = MapToDto(asistencia)
                };
            }

            // Marcar como presente (true)
            var updated = await attendanceService.EditAttendanceAsync(asistencia.Id, new AttendanceEditDto
            {
                Attended = true,
                Status = "Presente"
            });

            return new QRValidationResponseDto
            {
                Success = true,
                Message = "Asistencia validada correctamente",
                Attendance = updated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando QR");
            throw;
        }
    }

    private string GenerateQRCode(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeImage);
    }

    private AttendanceDto MapToDto(AttendanceEntity entity)
    {
        return new AttendanceDto
        {
            Id = entity.Id,
            Attended = entity.Attended,
            CourseId = entity.CourseId,
            StudentId = entity.StudentId,
            RegistrationDate = entity.RegistrationDate
        };
    }
}

// DTOs necesarios
public class QRGenerationResponseDto
{
    public string QRCode { get; set; }
    public DateTime ExpirationTime { get; set; }
    public string ValidationUrl { get; set; }
}

public class QRValidationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public AttendanceDto Attendance { get; set; }
}