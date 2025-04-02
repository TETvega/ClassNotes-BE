using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.QR;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Services.Distance;
using Microsoft.EntityFrameworkCore;
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
    private static DateTime _qrExpirationTime;
    private bool _isRunning = false;
    private bool _messageShown = false;
    private int _validationCount = 0;
    private Timer _expirationTimer;

    //DD: Lista temporal para almacenar IDs de estudiantes que han validado su asistencia
    private static List<Guid> _validatedStudentIds = new List<Guid>();

    //DD: ID de la clase actual activa
    private static Guid _currentCourseId;
    private static string _currentTeacherId;

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

    public async Task<object> GenerarQR(QRGenerationRequestDto request, string teacherId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

            //DD: Validación básica de parámetros
            if (request.TiempoLimiteMinutos < 1 || request.TiempoLimiteMinutos > 30)
                throw new ArgumentException("El tiempo límite debe estar entre 1 y 30 minutos.");

            if (request.RangoValidacionMetros < 15 || request.RangoValidacionMetros > 100)
                throw new ArgumentException("El rango de validación debe estar entre 15 y 100 metros.");

            //DD: Validación de la clase
            var curso = await context.Courses
                .Include(c => c.Students)
                    .ThenInclude(sc => sc.Student)
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId);

            if (curso == null)
                throw new ArgumentException("La clase no existe o no está asociada con el profesor y centro especificados.");

            var centroId = curso.CenterId;

            var centro = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centroId && c.TeacherId == teacherId);
            if (centro == null)
                throw new ArgumentException("No tienes permiso para generar QR para esta clase.");

            //DD: Limpiar lista de estudiantes validados anterior si existe
            _validatedStudentIds.Clear();

            //DD: Guardar el ID de la clase actual
            _currentCourseId = request.ClaseId;
            _currentTeacherId = teacherId;

            //DD: Generar contenido del QR - Simplificado
            var qrContent = $"{request.ClaseId}|{request.Latitud}|{request.Longitud}|{request.RangoValidacionMetros}";

            _qrExpirationTime = DateTime.UtcNow.AddMinutes(request.TiempoLimiteMinutos);
            _isRunning = true;
            _messageShown = false;

            //DD: Configurar timer para la expiración
            var tiempoRestante = _qrExpirationTime - DateTime.UtcNow;
            _expirationTimer = new Timer(async _ => await OnQRExpired(), null, tiempoRestante, Timeout.InfiniteTimeSpan);

            //DD: Generar QR con solo el contenido
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    var qrCodeBase64 = Convert.ToBase64String(qrCodeImage);
                    return new { QRCode = qrCodeBase64, CodeContent = qrContent };
                }
            }
        }
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
                //DD: Validar formato del QR
                var qrParts = request.QRContent.Split('|');
                if (qrParts.Length < 4) throw new ArgumentException("Código QR inválido.");

                if (!Guid.TryParse(qrParts[0], out Guid claseId))
                {
                    throw new ArgumentException("Código QR inválido: datos incorrectos.");
                }

                if (!double.TryParse(qrParts[1], out double qrLatitud) || !double.TryParse(qrParts[2], out double qrLongitud))
                {
                    throw new ArgumentException("Coordenadas del QR inválidas.");
                }

                if (!int.TryParse(qrParts[3], out int rangoValidacionMetros))
                {
                    throw new ArgumentException("Rango de validación del QR inválido.");
                }

                //DD: Validar distancia
                double distancia = distanceService.CalcularDistancia(qrLatitud, qrLongitud, request.EstudianteLatitud, request.EstudianteLongitud);

                if (distancia > rangoValidacionMetros)
                {
                    throw new ArgumentException($"El estudiante está fuera del rango de validación. Distancia: {distancia} metros, Rango permitido: {rangoValidacionMetros} metros.");
                }

                //DD: Obtener estudiante por correo
                var estudiante = await context.Students
                    .FirstOrDefaultAsync(s => s.Email == request.EstudianteCorreo);
                if (estudiante == null)
                {
                    throw new ArgumentException("El estudiante no está registrado.");
                }

                //DD: Verificar que el estudiante pertenece a la clase
                var estudianteEnClase = await context.StudentsCourses
                    .AnyAsync(sc => sc.StudentId == estudiante.Id && sc.CourseId == claseId && sc.IsActive);
                if (!estudianteEnClase)
                {
                    throw new ArgumentException("El estudiante no está asociado a esta clase.");
                }

                //DD: Buscar asistencia existente
                var asistencia = await context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == estudiante.Id && a.CourseId == claseId);

                if (asistencia == null)
                {
                    //DD: Crear nueva asistencia si no existe
                    var attendanceCreateDto = new AttendanceCreateDto
                    {
                        Attended = true,
                        Status = "Presente",
                        CourseId = claseId,
                        StudentId = estudiante.Id
                    };

                    var newAttendance = await attendanceService.CreateAttendanceAsync(attendanceCreateDto);

                    //DD: Añadir a la lista de validados
                    if (!_validatedStudentIds.Contains(estudiante.Id))
                    {
                        _validatedStudentIds.Add(estudiante.Id);
                    }

                    return new
                    {
                        Message = "Asistencia confirmada y registrada.",
                        Correo = request.EstudianteCorreo,
                        Attendance = new
                        {
                            Attended = true,
                            Status = "Presente"
                        }
                    };
                }
                else
                {
                    //DD: Verificar si la asistencia ya ha sido validada
                    if (asistencia.Status != "En espera")
                    {
                        return new
                        {
                            Message = "El estudiante ya ha sido marcado como presente o ausente.",
                            Correo = request.EstudianteCorreo,
                            Attendance = new
                            {
                                Id = asistencia.Id,
                                Attended = asistencia.Attended,
                                Status = asistencia.Status,
                                RegistrationDate = asistencia.RegistrationDate,
                                CourseId = asistencia.CourseId,
                                StudentId = asistencia.StudentId
                            }
                        };
                    }

                    //DD: Actualizar la asistencia a "Presente" y true
                    var attendanceEditDto = new AttendanceEditDto
                    {
                        Attended = true,
                        Status = "Presente"
                    };

                    var updatedAttendance = await attendanceService.EditAttendanceAsync(asistencia.Id, attendanceEditDto);

                    //DD: Añadir a la lista de validados
                    if (!_validatedStudentIds.Contains(estudiante.Id))
                    {
                        _validatedStudentIds.Add(estudiante.Id);
                    }

                    return new
                    {
                        Message = "Asistencia confirmada y registrada.",
                        Correo = request.EstudianteCorreo,
                        Attendance = new
                        {
                            Attended = updatedAttendance.Attended,
                            Status = updatedAttendance.Status
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el código QR.");
                throw;
            }
        }
    }

    private async Task OnQRExpired()
    {
        _logger.LogInformation("Procesando asistencias no validadas...");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

        try
        {
            //DD: Obtener profesor que generó el QR
            var profesor = await context.Users
                .FirstOrDefaultAsync(u => u.Id == _currentTeacherId);

            if (profesor == null)
            {
                _logger.LogError("Profesor no encontrado para auditoría");
                return;
            }

            //DD: Obtener estudiantes no validados
            var noValidados = await context.StudentsCourses
                .Where(sc => sc.CourseId == _currentCourseId && sc.IsActive)
                .Select(sc => sc.StudentId)
                .Where(id => !_validatedStudentIds.Contains(id))
                .ToListAsync();

            //DD: Registrar inasistencias
            foreach (var studentId in noValidados)
            {
                var asistencia = await context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == studentId && a.CourseId == _currentCourseId);

                if (asistencia == null)
                {
                    context.Attendances.Add(new AttendanceEntity
                    {
                        Attended = false,
                        Status = "No Presente",
                        CourseId = _currentCourseId,
                        StudentId = studentId,
                        CreatedByUser = profesor,
                        UpdatedByUser = profesor,
                        RegistrationDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    });
                }
            }

            //DD: Guardar con el usuario auditor correcto
            await context.SaveChangesWithoutAuditAsync();
            _validatedStudentIds.Clear();

            _logger.LogInformation($"Procesadas {noValidados.Count} inasistencias");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar QR expirado");
        }
    }
}