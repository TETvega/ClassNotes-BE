using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.QR;
using ClassNotes.API.Services.Attendances;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Microsoft.Extensions.DependencyInjection;

// Servicio para generar y validar códigos QR para la asistencia
public class QRService
{
    private readonly ClassNotesContext _context;
    private readonly IAttendancesService _attendanceService;
    private readonly IServiceProvider _serviceProvider;
    private static readonly HashSet<string> _validatedMACs = new HashSet<string>();
    private static readonly HashSet<string> _validatedEmails = new HashSet<string>();
    private static DateTime _qrExpirationTime;

    // Constructor que recibe las dependencias necesarias
    public QRService(ClassNotesContext context, IAttendancesService attendanceService, IServiceProvider serviceProvider)
    {
        _context = context;
        _attendanceService = attendanceService;
        _serviceProvider = serviceProvider;
    }

    // Método para generar un código QR
    public async Task<object> GenerateQR(QRGenerationRequestDto request)
    {
        // Validar el tiempo límite (mínimo 3 minutos)
        if (request.TiempoLimiteMinutos < 3)
        {
            throw new ArgumentException("El tiempo límite mínimo es de 3 minutos.");
        }

        // Validar el rango de validación (mínimo 15 metros, máximo 100 metros)
        if (request.RangoValidacionMetros < 15 || request.RangoValidacionMetros > 100)
        {
            throw new ArgumentException("El rango de validación debe estar entre 15 y 100 metros.");
        }

        // Validar si el profesor existe
        var profesor = await _context.Users.FindAsync(request.ProfesorId);
        if (profesor == null) throw new ArgumentException("El profesor no existe.");

        // Validar si el centro existe
        var centro = await _context.Centers.FindAsync(request.CentroId);
        if (centro == null) throw new ArgumentException("El centro no existe.");

        // Validar si la clase existe y está asociada con el profesor y el centro
        var clase = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.TeacherId == request.ProfesorId && c.CenterId == request.CentroId);
        if (clase == null) throw new ArgumentException("La clase no está asociada con el profesor o el centro.");

        // Generar la URL de validación con datos embebidos en el QR
        var timestamp = DateTime.UtcNow.ToString("o");
        var qrContent = $"{request.ProfesorId}|{request.CentroId}|{request.ClaseId}|{request.Latitud}|{request.Longitud}|{timestamp}|{request.TiempoLimiteMinutos}|{request.RangoValidacionMetros}";

        // Establecer el tiempo de expiración del QR
        _qrExpirationTime = DateTime.UtcNow.AddMinutes(request.TiempoLimiteMinutos);

        // Limpiar las listas de direcciones MAC y correos cuando el QR expire
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(request.TiempoLimiteMinutos));
            _validatedMACs.Clear();
            await ValidateAttendances(request.ClaseId);
        });

        // Generar la URL de validación
        string validationUrl = $"https://localhost:7047/api/QR/validate?qrContent={Uri.EscapeDataString(qrContent)}";

        // Generar código QR con la URL
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

    // Método para validar un código QR
    public async Task<object> ValidateQR(QRValidationRequestDto request)
    {
        // Validar el formato del QR
        var qrParts = request.QRContent.Split('|');
        if (qrParts.Length < 8) throw new ArgumentException("Código QR inválido.");

        // Parsear los datos del QR
        if (!Guid.TryParse(qrParts[1], out Guid centroId) ||
            !Guid.TryParse(qrParts[2], out Guid claseId) ||
            !double.TryParse(qrParts[3], out double latitud) ||
            !double.TryParse(qrParts[4], out double longitud) ||
            !DateTime.TryParse(qrParts[5], out DateTime qrTimestamp) ||
            !int.TryParse(qrParts[6], out int tiempoLimiteMinutos) ||
            !int.TryParse(qrParts[7], out int rangoValidacionMetros))
        {
            throw new ArgumentException("Código QR inválido: datos incorrectos.");
        }

        // Validar si el QR ha expirado
        if (DateTime.UtcNow > _qrExpirationTime)
        {
            throw new ArgumentException("El código QR ha expirado.");
        }

        // Buscar al estudiante por su correo electrónico
        var estudiante = await _context.Students
            .FirstOrDefaultAsync(s => s.Email == request.EstudianteCorreo);
        if (estudiante == null)
        {
            throw new ArgumentException("El estudiante no está registrado.");
        }

        // Verificar si la dirección MAC ya ha validado su asistencia
        if (_validatedMACs.Contains(request.MacAddress))
        {
            throw new ArgumentException("Ya has validado tu asistencia desde este dispositivo.");
        }

        // Crear el DTO para la asistencia (sin la dirección MAC)
        var attendanceCreateDto = new AttendanceCreateDto
        {
            Attended = "Presente",
            CourseId = claseId,
            StudentId = estudiante.Id
        };

        // Crear la asistencia utilizando el servicio
        var attendanceDto = await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);

        // Agregar la dirección MAC y el correo a las listas de validados
        _validatedMACs.Add(request.MacAddress);
        _validatedEmails.Add(request.EstudianteCorreo);

        // Retornar la respuesta con los datos del estudiante y la asistencia
        return new
        {
            Message = "Asistencia confirmada y registrada.",
            Estudiante = request.EstudianteNombre,
            Correo = request.EstudianteCorreo,
            Attendance = attendanceDto
        };
    }

    // Método para validar las asistencias cuando el QR expira
    private async Task ValidateAttendances(Guid claseId)
    {
        try
        {
            // Crear un nuevo ámbito dentro del hilo secundario
            using (var scope = _serviceProvider.CreateScope())
            {
                var newContext = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

                // Obtener la lista de estudiantes de la clase
                var estudiantesClase = await newContext.Students
                    .Where(s => s.Courses.Any(c => c.Id == claseId))
                    .Select(s => s.Email)
                    .ToListAsync();

                // Comparar con los correos que validaron su asistencia
                foreach (var email in estudiantesClase)
                {
                    var attendanceCreateDto = new AttendanceCreateDto
                    {
                        Attended = _validatedEmails.Contains(email) ? "Presente" : "No Presente",
                        CourseId = claseId,
                        StudentId = (await newContext.Students.FirstOrDefaultAsync(s => s.Email == email)).Id
                    };

                    // Crear o actualizar la asistencia
                    await _attendanceService.CreateAttendanceAsync(attendanceCreateDto);
                }

                // Limpiar la lista de correos validados
                _validatedEmails.Clear();
            }
        }
        catch (Exception ex)
        {
            // Manejar errores (puedes loguear el error)
            Console.WriteLine($"Error al validar asistencias: {ex.Message}");
        }
    }
}