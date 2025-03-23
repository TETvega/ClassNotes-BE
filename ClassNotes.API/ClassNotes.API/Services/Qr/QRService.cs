using ClassNotes.API.Database;
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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public class QRService : BackgroundService
{
    private readonly ILogger<QRService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static DateTime _qrExpirationTime;
    private bool _isRunning = false;
    private bool _messageShown = false;
    private int _validationCount = 0;
    private Timer _expirationTimer;

    //DD: Lista en memoria para almacenar direcciones MAC permitidas
    private static List<string> _macAddressesPermitidas = new List<string>();

    public QRService(
        ILogger<QRService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de limpieza de QR iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_isRunning)
                {
                    if (!_messageShown)
                    {
                        _logger.LogInformation("El servicio de limpieza de QR está inactivo. Esperando reactivación...");
                        _messageShown = true;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                _validationCount++;
                _logger.LogInformation($"Validación #{_validationCount} realizada.");
                Console.WriteLine($"Validación #{_validationCount} realizada.");

                if (DateTime.UtcNow > _qrExpirationTime)
                {
                    _logger.LogInformation("El código QR ha expirado. Validando asistencias...");
                    Console.WriteLine("El código QR ha expirado.");

                    await OnQRExpired();

                    _logger.LogInformation("Asistencias validadas y lista de estudiantes limpiada.");
                    _isRunning = false;

                    Console.WriteLine("Proceso de validación de asistencias completado.");
                }
                else
                {
                    var tiempoRestante = _qrExpirationTime - DateTime.UtcNow;
                    _logger.LogInformation($"El código QR aún no ha expirado. Tiempo restante: {tiempoRestante}");
                    Console.WriteLine($"El código QR aún no ha expirado. Tiempo restante: {tiempoRestante}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar asistencias.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Servicio de limpieza de QR detenido.");
    }

    private async Task ValidateAttendancesUntilNonePending(ClassNotesContext context, IAttendancesService attendanceService)
    {
        try
        {
            bool pendingAttendancesExist;

            do
            {
                var asistenciasEnEspera = await context.Attendances
                    .Where(a => a.Attended == "En espera")
                    .ToListAsync();

                foreach (var asistencia in asistenciasEnEspera)
                {
                    var attendanceEditDto = new AttendanceEditDto
                    {
                        Attended = "No Presente"
                    };

                    await attendanceService.EditAttendanceAsync(asistencia.Id, attendanceEditDto);
                }

                pendingAttendancesExist = await context.Attendances
                    .AnyAsync(a => a.Attended == "En espera");

                if (pendingAttendancesExist)
                {
                    _logger.LogInformation("Aún hay asistencias en espera. Continuando la validación...");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }

            } while (pendingAttendancesExist);

            _logger.LogInformation("Todas las asistencias han sido validadas.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar asistencias.");
        }
    }

    public async Task<object> GenerateQR(QRGenerationRequestDto request)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();

            if (request.TiempoLimiteMinutos < 1 || request.TiempoLimiteMinutos > 30)
                throw new ArgumentException("El tiempo límite debe estar entre 1 y 30 minutos.");
            if (request.RangoValidacionMetros < 15 || request.RangoValidacionMetros > 100)
                throw new ArgumentException("El rango de validación debe estar entre 15 y 100 metros.");

            var profesor = await context.Users.FindAsync(request.ProfesorId);
            if (profesor == null) throw new ArgumentException("El profesor no existe.");

            var centro = await context.Centers.FindAsync(request.CentroId);
            if (centro == null) throw new ArgumentException("El centro no existe.");

            var clase = await context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.TeacherId == request.ProfesorId && c.CenterId == request.CentroId);
            if (clase == null) throw new ArgumentException("La clase no está asociada con el profesor o el centro.");

            var estudiantesClase = await context.StudentsCourses
                .Where(sc => sc.CourseId == request.ClaseId && sc.IsActive)
                .Select(sc => sc.Student)
                .ToListAsync();

            foreach (var estudiante in estudiantesClase)
            {
                var attendanceCreateDto = new AttendanceCreateDto
                {
                    Attended = "En espera",
                    CourseId = request.ClaseId,
                    StudentId = estudiante.Id
                };

                await attendanceService.CreateAttendanceAsync(attendanceCreateDto);
            }

            var timestamp = DateTime.UtcNow.ToString("o");
            var qrContent = $"{request.ProfesorId}|{request.CentroId}|{request.ClaseId}|{request.Latitud}|{request.Longitud}|{timestamp}|{request.TiempoLimiteMinutos}|{request.RangoValidacionMetros}|{request.PermitirMultiplesDispositivos}";

            _qrExpirationTime = DateTime.UtcNow.AddMinutes(request.TiempoLimiteMinutos);
            _logger.LogInformation($"Tiempo de expiración del QR: {_qrExpirationTime}");

            _isRunning = true;
            _messageShown = false;

            var tiempoRestante = _qrExpirationTime - DateTime.UtcNow;
            _expirationTimer = new Timer(async _ => await OnQRExpired(), null, tiempoRestante, Timeout.InfiniteTimeSpan);

            string validationUrl = $"https://localhost:7047/api/QR/validate?qrContent={Uri.EscapeDataString(qrContent)}";

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(validationUrl, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    var qrCodeBase64 = Convert.ToBase64String(qrCodeImage);
                    return new { QRCode = qrCodeBase64, ValidationUrl = validationUrl };
                }
            }
        }
    }

    private async Task OnQRExpired()
    {
        _logger.LogInformation("El código QR ha expirado. Validando asistencias...");
        Console.WriteLine("El código QR ha expirado.");

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();

            await ValidateAttendancesUntilNonePending(context, attendanceService);
        }

        // Limpiar la lista de direcciones MAC permitidas cuando el QR expire
        _macAddressesPermitidas.Clear();
        _logger.LogInformation("Lista de direcciones MAC permitidas limpiada.");

        _logger.LogInformation("Asistencias validadas y lista de estudiantes limpiada.");
        _isRunning = false;

        Console.WriteLine("Proceso de validación de asistencias completado.");
    }

    public async Task<object> ValidateQR(QRValidationRequestDto request)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendancesService>();
            var distanceService = scope.ServiceProvider.GetRequiredService<DistanceService>();

            try
            {
                var qrParts = request.QRContent.Split('|');
                if (qrParts.Length < 9) throw new ArgumentException("Código QR inválido.");

                if (!Guid.TryParse(qrParts[2], out Guid claseId))
                {
                    throw new ArgumentException("Código QR inválido: datos incorrectos.");
                }

                if (!double.TryParse(qrParts[3], out double qrLatitud) || !double.TryParse(qrParts[4], out double qrLongitud))
                {
                    throw new ArgumentException("Coordenadas del QR inválidas.");
                }

                if (!int.TryParse(qrParts[7], out int rangoValidacionMetros))
                {
                    throw new ArgumentException("Rango de validación del QR inválido.");
                }

                double distancia = distanceService.CalcularDistancia(qrLatitud, qrLongitud, request.EstudianteLatitud, request.EstudianteLongitud);

                if (distancia > rangoValidacionMetros)
                {
                    throw new ArgumentException($"El estudiante está fuera del rango de validación. Distancia: {distancia} metros, Rango permitido: {rangoValidacionMetros} metros.");
                }

                var estudiante = await context.Students
                    .FirstOrDefaultAsync(s => s.Email == request.EstudianteCorreo);
                if (estudiante == null)
                {
                    throw new ArgumentException("El estudiante no está registrado.");
                }

                var estudianteEnClase = await context.StudentsCourses
                    .AnyAsync(sc => sc.StudentId == estudiante.Id && sc.CourseId == claseId && sc.IsActive);
                if (!estudianteEnClase)
                {
                    throw new ArgumentException("El estudiante no está asociado a esta clase.");
                }

                if (!string.IsNullOrEmpty(request.MacAddress))
                {
                    //DD: Verificar si la dirección MAC ya ha sido usada
                    if (_macAddressesPermitidas.Contains(request.MacAddress))
                    {
                        throw new ArgumentException("El dispositivo ya ha sido usado para validar la asistencia.");
                    }

                   
                    _macAddressesPermitidas.Add(request.MacAddress);
                }

                var asistencia = await context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == estudiante.Id && a.CourseId == claseId);
                if (asistencia == null)
                {
                    throw new ArgumentException("No se encontró la asistencia del estudiante.");
                }

                //DD Verificar si la asistencia ya ha sido validada
                if (asistencia.Attended != "En espera")
                {
                    return new
                    {
                        Message = "El estudiante ya ha sido marcado como presente o ausente.",
                        Correo = request.EstudianteCorreo,
                        Attendance = new AttendanceDto 
                        {
                            Id = asistencia.Id,
                            Attended = asistencia.Attended,
                            RegistrationDate = asistencia.RegistrationDate,
                            CourseId = asistencia.CourseId,
                            StudentId = asistencia.StudentId
                        }
                    };
                }

                //DD: Actualizar la asistencia a "Presente"
                var attendanceEditDto = new AttendanceEditDto
                {
                    Attended = "Presente"
                };

                var updatedAttendance = await attendanceService.EditAttendanceAsync(asistencia.Id, attendanceEditDto);

                //DD: Mapea la asistencia actualizada a un DTO
                var attendanceDto = new AttendanceDto
                {
                    Id = updatedAttendance.Id,
                    Attended = updatedAttendance.Attended,
                    RegistrationDate = updatedAttendance.RegistrationDate,
                    CourseId = updatedAttendance.CourseId,
                    StudentId = updatedAttendance.StudentId
                };

                return new
                {
                    Message = "Asistencia confirmada y registrada.",
                    Correo = request.EstudianteCorreo,
                    Attendance = attendanceDto 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el código QR.");
                throw;
            }
        }
    }
} 