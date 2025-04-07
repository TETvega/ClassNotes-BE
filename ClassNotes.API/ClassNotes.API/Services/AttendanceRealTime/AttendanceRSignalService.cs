using AutoMapper;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Hubs;
using ClassNotes.API.Services.Audit;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Services.Otp;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Org.BouncyCastle.Asn1.Ocsp;
using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebRequestMethods;

namespace ClassNotes.API.Services.AttendanceRealTime
{
    public class AttendanceRSignalService: IAttendanceRSignalService
    {
        private readonly ClassNotesContext _context;
        private readonly IAuditService _auditService;
        private readonly IHubContext<AttendanceHub> _hubContext;
        private readonly IEmailsService _emailsService;
        private readonly IOtpService _otpService;
        private readonly IMapper _mapper;

        public AttendanceRSignalService(
            ClassNotesContext context,
            IAuditService auditService,
            IHubContext<AttendanceHub> hubContext,
            IEmailsService emailsService,
            IOtpService otpService,
            IMapper mapper

            )
        {
            _context = context;
            _auditService = auditService;
            _hubContext = hubContext;
            _emailsService = emailsService;
            _otpService = otpService;
            _mapper = mapper;
        }
        //public async Task<ResponseDto> ValidateAttendanceAsync(ValidateOtpDto dto)
        //{
        //    var validOtp = _otpCache.Get(dto.OTP); // Revisa que sea válido
        //    if (validOtp == null || validOtp.StudentId != dto.StudentId)
        //    {
        //        return new ResponseDto { Status = false, Message = "OTP inválido" };
        //    }

        //    var attendance = await _context.Attendances
        //        .FirstOrDefaultAsync(a => a.StudentId == dto.StudentId && a.CourseId == dto.CourseId);

        //    if (attendance == null)
        //    {
        //        return new ResponseDto { Status = false, Message = "Asistencia no encontrada" };
        //    }

        //    attendance.Attended = true;
        //    attendance.Status = "PRESENT";
        //    attendance.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    await _hubContext.Clients.Group(dto.CourseId.ToString())
        //        .SendAsync("UpdateAttendanceStatus", new
        //        {
        //            studentId = dto.StudentId,
        //            status = "PRESENT"
        //        });

        //    return new ResponseDto { Status = true, Message = "Asistencia registrada correctamente." };
        //}

        public async Task<ResponseDto<object>> ProcessAttendanceAsync(AttendanceRequestDto request)
        {
            var userId = _auditService.GetUserId();
            // Traemos la información del curso, su centro y los estudiantes activos
            var course = await _context.Courses
                 .Where(c => c.Id == request.CourseId &&
                             c.IsActive &&
                             c.Center.IsArchived == false &&
                             c.Center.TeacherId == userId)
                 .Include(c => c.Center)
                 .Include(c => c.Students.Where(sc => sc.IsActive))
                     .ThenInclude(sc => sc.Student)
                 .FirstOrDefaultAsync();


            // Validaciones
            if (course == null)
            {
                return new ResponseDto<object>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = "El curso no fue encontrado, no está activo o no pertenece al docente.",
                    Data = null
                };
            }


            if (!(request.AttendanceType.Email || request.AttendanceType.Qr))
            {
                return new ResponseDto<object>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Debe seleccionar al menos un tipo de registro de asistencia (email o QR).",
                    Data = null
                };
            }

            // Validaciones de 
            Point locationToUse = null;

            if (!request.HomePlace)
            {
                if (request.NewGeolocation == null)
                {
                    return new ResponseDto<object>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "La ubicación proporcionada (NewGeolocation) es requerida cuando 'HomePlace' esta desactivado",
                        Data = null
                    };
                }

                locationToUse =  _mapper.Map<Point>(request.NewGeolocation);
            }

            var courseSetting = await _context.CoursesSettings
                .Where(cs => cs.Id == course.SettingId)
                .FirstOrDefaultAsync();
            if (courseSetting == null)
            {
                return new ResponseDto<object>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "No se Encontro una Configuracion Por Defecto",
                    Data = null
                };
            }

            if (courseSetting.GeoLocation == null)
            {
                return new ResponseDto<object>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "No se encontró una ubicación predeterminada configurada para el curso.",
                    Data = null
                };
            }
            if (request.HomePlace)
            {
                locationToUse = courseSetting.GeoLocation;
            }
            if (request.AttendanceType.Email)
            {
                foreach (var sc in course.Students)
                {
                    var student = sc.Student;
                    var secretKey = _otpService.GenerateSecretKey(student.Email.ToString(), student.Id.ToString());

                    // CG: Guardar el OTP en memoria
                    var otpCode = _otpService.GenerateOtp(secretKey, courseSetting.MinimumAttendanceTime);
                    var emailDto = CreateEmailDto(student, course, otpCode, courseSetting.MinimumAttendanceTime);
                    //await _emailsService.SendEmailAsync(emailDto);
                }
            }
            // Generar QR si se seleccionó
            string qrBase64 = null;
            string qrContent = null;

            if (request.AttendanceType.Qr)
            {
                var expirationTime = DateTime.UtcNow.AddMinutes(courseSetting.MinimumAttendanceTime);
                var expirationString = expirationTime.ToString("o"); 
                var content = $"{course.Id}|{locationToUse.X}|{locationToUse.Y}|{request.StrictMode}|{courseSetting.ValidateRangeMeters}|{expirationString}";

                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(20);
                        qrBase64 = Convert.ToBase64String(qrCodeImage);
                        qrContent = content;
                    }
                }

            }

            // Registrar asistencia en estado 'WAITING'
            foreach (var student in course.Students)
            {
                var attendance = new AttendanceEntity
                {
                    CourseId = course.Id,
                    StudentId = student.StudentId,
                    Attended = false,
                    Status = "WAITING",
                    RegistrationDate = DateTime.UtcNow
                };

                _context.Attendances.Add(attendance);

                await _hubContext.Clients.Group(course.Id.ToString())
                    .SendAsync("UpdateAttendanceStatus", new
                    {
                        studentId = student.StudentId,
                        status = "WAITING"
                    });
            }

            await _context.SaveChangesAsync();


            // Mapeamos la respuesta con los datos requeridos
            var result = new
            {
                Course = new
                {
                    course.Id,
                    course.Name,
                    course.Code,
                    course.Section
                },
                Center = new
                {
                    course.Center.Name,
                    course.Center.Abbreviation
                },
                Students = course.Students.Select(sc => new
                {
                    sc.Student.Id,
                    sc.Student.FirstName,
                    sc.Student.LastName,
                    sc.Student.Email
                }).ToList(),
                Qr = request.AttendanceType.Qr ? new
                {
                    Base64 = qrBase64,
                    Content = qrContent
                } : null
            };

            return new ResponseDto<object>
            {
                StatusCode = 200,
                Status = true,
                Message = "Asistencia procesada exitosamente.",
                Data = result
            };
        }

        private EmailDto CreateEmailDto(StudentEntity estudiante, CourseEntity clase, string otp, int tiempoExpiracion)
        {
            return new EmailDto
            {
                To = estudiante.Email,
                Subject = "📌 Código de Validación de Asistencia",
                Content = $@"
            <div style='font-family: Arial, sans-serif; text-align: center;'>
                <h2 style='color: #4A90E2;'>👋 Hola {estudiante.FirstName},</h2>
                <p style='font-size: 16px; color: #333;'>
                    Para validar tu asistencia a la clase <strong>{clase.Name}</strong>, usa el siguiente código:
                </p>
                <div style='display: inline-block; background: #EAF3FF; padding: 15px; border-radius: 8px; font-size: 24px; font-weight: bold; letter-spacing: 3px;'>
                    {otp}
                </div>
                <p style='margin-top: 20px;'>
                    O puedes hacer clic en el siguiente botón para validar tu asistencia automáticamente:
                </p>
                <a href='https://tudominio.com/asistencia/{otp}' 
                   style='display: inline-block; background: #4A90E2; color: white; padding: 10px 20px; 
                          text-decoration: none; border-radius: 5px; font-size: 18px;'>
                    ✅ Validar Asistencia
                </a>
                <p style='font-size: 14px; color: #777; margin-top: 20px;'>
                    Este código es válido por {tiempoExpiracion} minutos.
                </p>
            </div>"
            };
        }
    }
}
