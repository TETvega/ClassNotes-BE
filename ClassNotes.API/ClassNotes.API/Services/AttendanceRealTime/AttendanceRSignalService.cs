using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.AttendacesRealTime.ForStudents;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Hubs;
using ClassNotes.API.Models;
using ClassNotes.API.Services.Audit;
using ClassNotes.API.Services.ConcurrentGroups;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Services.Otp;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtpNet;
using ProjNet.CoordinateSystems;
using QRCoder;
using System.Collections.Concurrent;
using static QRCoder.PayloadGenerator;
using Point = NetTopologySuite.Geometries.Point;


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
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILoggerFactory _logger;
        private readonly ConcurrentDictionary<Guid, AttendanceGroupCache> _groupCache;
        private readonly IAttendanceGroupCacheManager _groupCacheManager;

        public AttendanceRSignalService(
            ClassNotesContext context,
            IAuditService auditService,
            IHubContext<AttendanceHub> hubContext,
            IEmailsService emailsService,
            IOtpService otpService,
            IMapper mapper,
            IMemoryCache cache,
            IServiceScopeFactory serviceScopeFactory,
            ILoggerFactory logger,
            ConcurrentDictionary<Guid, AttendanceGroupCache> groupCache,
            IAttendanceGroupCacheManager groupCacheManager

            )
        {
            _context = context;
            _auditService = auditService;
            _hubContext = hubContext;
            _emailsService = emailsService;
            _otpService = otpService;
            _mapper = mapper;
            _cache = cache;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _groupCache = groupCache;
            _groupCacheManager = groupCacheManager;
        }
        ///<summary>
        ///    Procesa la asistencia de estudiantes a un curso, generando códigos QR y/o OTP por email según configuración.
        ///</summary>
        ///<remarks>
        ///    Este endpoint realiza las siguientes operaciones principales:
        ///    1. Valida que el curso exista, esté activo y pertenezca al docente
        ///    2. Verifica que se haya seleccionado al menos un método de asistencia (QR o email)
        ///    3. Determina la ubicación a usar para validar asistencia(geolocalización predeterminada o nueva)
        ///    4. Genera códigos QR y/o OTP según configuración
        ///    5. Almacena temporalmente la información en caché para validación posterior
        ///    6. Notifica a los clientes conectados via SignalR sobre el estado de asistencia
        ///</remarks>
        ///  <param name = "request" > Objeto con los parámetros de la solicitud de asistencia</param>
        ///<returns>
        ///    Objeto ResponseDto con:
        ///    - StatusCode: 200 si éxito, códigos de error en caso contrario
        ///    - Data: Información del curso, estudiantes y QR generado (si aplica)
        ///</returns>
        ///    <response code = "200" > Asistencia procesada correctamente</response>
        ///    <response code = "400" > Error en parámetros de entrada o configuración</response>
        ///    <response code = "404" > Curso no encontrado o no accesible</response>
        /// Si quiere entender correctamente este endpoint le suguiero ver los siguientes enlaces
        /// para entendimiento de memoria https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-6.0
        /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryextensions.registerpostevictioncallback?view=net-9.0-pp
        /// metodos aplicables a la cache https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions?view=net-9.0-pp&viewFallbackFrom=dotnet-plat-ext-6.0
        /// enviar mensajes fuera del hub 
        /// https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext?view=aspnetcore-6.0
        /// 




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
            var courseKey = $"attendance_active_{course.Id}";

            if (_cache.TryGetValue(courseKey, out _))
            {
                return new ResponseDto<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = "Ya hay una asistencia activa en este curso.",
                };
            }



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
            if( request.StrictMode &&  request.AttendanceType.Qr && request.AttendanceType.Email)
            {
                return new ResponseDto<object>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "En modo Estricto solo puede seleccionar un Metodo para eitar asistencias cruzadas entre estudiantes",
                    Data = null
                };
            }

            
            
            Point locationToUse = null;
            // Determina qué ubicación usar para validar asistencia:
            // - Si es HomePlace: usa la geolocalización predeterminada del curso
            // - Si no es HomePlace: usa la nueva geolocalización proporcionada
            // - Valida que se proporcione nueva ubicación si no es HomePlace
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

            
            if (request.HomePlace)
            {
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
                locationToUse = courseSetting.GeoLocation;
            }
            // Generar QR si se seleccionó
            string qrBase64 = null;
            string qrContent = null;

            DateTime expiration = DateTime.Now.AddMinutes(courseSetting.MinimumAttendanceTime);
            // marcamos como activa con duración hasta el vencimiento
            var studentsList = course.Students
                .Select(sc => new AttendanceStudentStatus
                {
                    StudentId = sc.StudentId,
                    FullName = $"{sc.Student.FirstName} {sc.Student.LastName}",
                    Email = sc.Student.Email,
                    Status = MessageConstant_Attendance.WAITING // todos inician en espera
                }).ToList();


            // Guardado en cache para recuperacion del docente cada que entra al endpoint 
            SaveActiveAttendanceToCache(
                courseKey,
                userId,
                course.Id,
                request.StrictMode,
                request.AttendanceType,
                expiration.AddMinutes(2),// agrege 2 minutos para manejar un desface de tiempo y no tener problemas con otps buscados
                studentsList
                );


            if (request.AttendanceType.Qr && request.StrictMode)
            {
                InitializeMacControlCache(course.Id, expiration);
            }



            // Creamos el Qr por si se va utilizar mas adelante
            // Formato: "courseId|X|Y|strictMode|validateRangeMeters|expiration"
            // Esto permite que el QR contenga toda la información necesaria para validar
            // la asistencia cuando sea escaneado posteriormente
            if (request.AttendanceType.Qr)
            {
                qrContent = $"{course.Id}|{locationToUse.X}|{locationToUse.Y}|{request.StrictMode}|{courseSetting.ValidateRangeMeters}|{expiration}";

                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                qrBase64 = Convert.ToBase64String(qrCodeImage);
            }

            // Procesamiento por cada estudiante:
            // - Genera OTP si está habilitado el método por email
            // - Almacena en caché la información temporal de asistencia
            // - Configura callback para manejar expiración (marca como no presente si no confirma)
            var groupCache = new AttendanceGroupCache
            {
                ExpirationTime = expiration,
                UserId = userId
            };

            foreach (var sc in course.Students)
            {
                var student = sc.Student;
                string otpCode = null;

                if (request.AttendanceType.Email)
                {
                    var secretKey = _otpService.GenerateSecretKey(student.Email.ToString(), student.Id.ToString());
                    otpCode = _otpService.GenerateOtp(secretKey, courseSetting.MinimumAttendanceTime);

                    var emailDto = CreateEmailDto(student, course, otpCode, courseSetting.MinimumAttendanceTime);
                    await _emailsService.SendEmailAsync(emailDto);
                }

                var memoryEntry = new TemporaryAttendanceEntry
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    Otp = otpCode,
                    QrContent = qrContent,
                    ExpirationTime = expiration,
                    Email = student.Email,
                    GeolocationLatitud = (float)(locationToUse?.Y?? 0f),
                    GeolocationLongitud = (float)(locationToUse?.X?? 0f) 
                };

                //SetStudentAttendanceCache( userId, student, course.Id, memoryEntry, expiration);
                groupCache.Entries.Add(memoryEntry);
                await _hubContext.Clients
                    .Group(course.Id.ToString())
                    .SendAsync(Attendance_Helpers.UPDATE_ATTENDANCE_STATUS, new
                    {
                        studentId = student.Id,
                        status = MessageConstant_Attendance.WAITING
                    });
            }
             _groupCacheManager.RegisterGroup(course.Id, groupCache);
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
                    sc.Student.Email,
                    status = MessageConstant_Attendance.WAITING
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
                <a href='https://tudominio.com/asistencia/{otp}-{clase.Id}' 
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
        /// <summary>
        /// Establece en caché temporal los datos de asistencia de un estudiante para un curso específico,
        /// con manejo automático de ausencias cuando expira el registro no marcado.
        /// </summary>
        /// <param name="userId">ID del usuario docente que realiza la operación (para auditoría)</param>
        /// <param name="student">Entidad del estudiante con sus datos completos</param>
        /// <param name="courseId">ID del curso relacionado</param>
        /// <param name="memoryEntry">Datos temporales de asistencia a almacenar</param>
        /// <param name="expiration">Fecha/hora de expiración del registro</param>
        /// <remarks>
        /// Comportamiento clave:
        /// - Crea una entrada en caché por cada par estudiante-curso
        /// - Registra automáticamente ausencia si el estudiante no marca asistencia antes de la expiración
        /// - Notifica a clientes conectados via SignalR cuando ocurren cambios
        /// 
        /// Estructura de la clave: "{studentId}_{courseId}"
        /// 
        /// Flujo de expiración:
        /// 1. Al expirar, verifica si no hubo check-in (IsCheckedIn=false)
        /// 2. Registra automáticamente como "NO PRESENTE" en base de datos
        /// 3. Notifica a todos los dispositivos suscritos al grupo del curso
        /// </remarks>
        private void SetStudentAttendanceCache(
            string userId,
            StudentEntity student,
            Guid courseId,
            TemporaryAttendanceEntry memoryEntry,
            DateTime expiration)
        {
            var memoryKey = $"{student.Id}_{courseId}";

            _cache.Set(
                memoryKey,
                memoryEntry,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiration,
                    PostEvictionCallbacks =
                    {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = async (key, value, reason, state) =>
                    {
                        if (reason == EvictionReason.Expired)
                        {
                            var data = (TemporaryAttendanceEntry)value;

                            if (!data.IsCheckedIn)
                            {
                                using var scope = _serviceScopeFactory.CreateScope();
                                var db = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

                                var missed = new AttendanceEntity
                                {
                                    CourseId = data.CourseId,
                                    StudentId = data.StudentId,
                                    Attended = false,
                                    Status = MessageConstant_Attendance.NOT_PRESENT,
                                    RegistrationDate = DateTime.UtcNow,
                                    CreatedBy = userId,
                                    CreatedDate = DateTime.UtcNow,
                                    Method = Attendance_Helpers.TYPE_MANUALLY,
                                    ChangeBy = Attendance_Helpers.SYSTEM // marcado como sistema solo para el manejo de logs a futuro

                                    
                                };

                                db.Attendances.Add(missed);
                                await db.SaveChangesWithoutAuditAsync();

                                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AttendanceHub>>();
                                // Notificar a todos los dispositivos suscritos
                                await hubContext.Clients.Group(data.CourseId.ToString())
                                    .SendAsync(Attendance_Helpers.UPDATE_ATTENDANCE_STATUS, new
                                    {
                                        studentId = data.StudentId,
                                        status = MessageConstant_Attendance.NOT_PRESENT
                                    });
                                
                            }

                            
                        }
                    }
                }
                    }
                });
        }
        /// <summary>
        /// Almacena temporalmente en caché la lista de estudiantes y configuración de asistencia para un curso,
        /// permitiendo el procesamiento posterior de registros de asistencia.
        /// </summary>
        /// <param name="cacheKey">Clave única para identificación en caché (formato recomendado: "attendance_active_{courseId}_{userId}")</param>
        /// <param name="courseId">Identificador único del curso asociado</param>
        /// <param name="strictMode">Habilita validaciones estrictas de geolocalización/temporización cuando es true</param>
        /// <param name="attendanceType">Configuración de métodos permitidos para registro (Email/QR/Ambos)</param>
        /// <param name="expiration">Fecha/hora de expiración automática de la caché (normalmente fin de la sesión de clase)</param>
        /// <param name="studentsList">Lista de estudiantes con sus estados actuales de asistencia</param>
        /// <remarks>
        /// Estructura de almacenamiento:
        /// - Los datos se guardan como objeto <see cref="ActiveAttendanceCacheDto"/>
        /// - La expiración es absoluta según el horario de fin de clase
        /// - No incluye callbacks de limpieza ya que es autónomo
        /// 
        /// Tipos de método:
        /// - "EMAIL": Solo verificación por correo
        /// - "QR": Solo código QR
        /// - "BOTH": Requiere ambos métodos simultáneamente
        /// </remarks>
        private void SaveActiveAttendanceToCache(
            string cacheKey,
            string userId,
            Guid courseId,
            bool strictMode,
            AttendanceTypeDto attendanceType,
            DateTime expiration,
            List<AttendanceStudentStatus> studentsList)
        {
            var method = attendanceType.Email && attendanceType.Qr ? "BOTH"
                       : attendanceType.Email ? "EMAIL"
                       : "QR";

            var cacheData = new ActiveAttendanceCacheDto
            {
                CourseId = courseId,
                UserId = userId,
                StrictMode = strictMode,
                AttendanceMethod = method,
                Expiration = expiration,
                Students = studentsList
            };

            _cache.Set(cacheKey, cacheData, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiration
                // No hace falta PostEvictionCallback aquí, solo queremos que se elimine de memoria
            });
        }
        /// <summary>
        /// Inicializa la caché de control MAC (Message Authentication Code) para un curso específico.
        /// </summary>
        /// <param name="courseId">Identificador único del curso</param>
        /// <param name="expiration">Fecha y hora de expiración para la entrada en caché</param>
        /// <remarks>
        /// Esta función gestiona un diccionario en caché para controlar tokens MAC globales por curso.
        /// Cuando la entrada expira, se ejecuta automáticamente una limpieza mediante callback.
        /// Solo cuando el modo estricto esta activado 
        /// 
        /// Estructura de la caché:
        /// - Clave: Formato "mac_global_{courseId}"
        /// - Valor: Dictionary(string, Guid) para almacenar tokens MAC asociados
        /// </remarks>
        private void InitializeMacControlCache(Guid courseId, DateTime expiration)
        {
            var macControlKey = $"mac_global_{courseId}";

            if (!_cache.TryGetValue(macControlKey, out _))
            {
                var macDictionary = new Dictionary<string, Guid>();

                _cache.Set(macControlKey, macDictionary, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiration,
                    PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        if (reason == EvictionReason.Expired)
                        {
                            var expiredCourseId = (Guid)state;
                            _logger.CreateLogger("CacheLogger")
                                .LogInformation($"MAC cache expirado para curso {expiredCourseId}");
                        }
                    },
                    State = courseId
                }
            }
                });
            }
        }


        public async Task<ResponseDto<StudentAttendanceResponse>> SendAttendanceByOtpAsync(
            string email,
            string OTP,
            float x,
            float y ,
            Guid courseId)
        {
            try
            {
                //var groupCacheKey = courseId;
               // var activeAttendance = _cache.Get<AttendanceGroupCache>(groupCacheKey);
                var activeAttendance = _groupCacheManager.GetGroupCache(courseId);

                if (activeAttendance == null)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 404,
                        Status = false,
                        Message = "No hay asistencia activa para este curso.",
                        Data = null
                    };
                }
                // datos del estudiante 
                var student = await _context.Students
                    .Include(s => s.Courses
                        .Where(sc => sc.CourseId == courseId && sc.IsActive)) // Filtras aquí
                    .ThenInclude(sc => sc.Course) // Solo accedes a la propiedad Course
                    .FirstOrDefaultAsync(s => s.Email == email);
                var courseName = student?.Courses.FirstOrDefault()?.Course?.Name ?? "No hay nombre";

                if (student == null)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 404,
                        Status = false,
                        Message = "Estudiante no encontrado o no está inscrito en el curso.",
                        Data = null
                    };
                }

                // lista de estudiantes en memoria 
                var studentEntry = _groupCacheManager.TryGetStudentEntryByEmail(courseId, email);
                if (studentEntry == null || studentEntry.IsCheckedIn == true)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "El estudiante no está registrado en la lista de asistencia o ya ha sido marcado.",
                        Data = null
                    };
                }

                if (studentEntry.Otp != OTP)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "OTP inválido o expirado.",
                        Data = null
                    };
                }

                // Validar ubicación
                var cachedLocation = new Point(studentEntry.GeolocationLongitud, studentEntry.GeolocationLatitud)
                {
                    SRID = 4326
                };
                var receivedLocation = new Point(x, y)
                {
                    SRID = 4326
                };

                var course = await _context.Courses
                    .Include(c => c.CourseSetting) // este es el CourseSettingEntity
                    .FirstOrDefaultAsync(c => c.Id == studentEntry.CourseId);

                var courseSetting = course?.CourseSetting;
                if (courseSetting == null)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "No se encontró configuración de asistencia para este curso.",
                        Data = null
                    };
                }

                // calculo de distacias
                double distanceInMeters = CalculateHaversineDistance(cachedLocation, receivedLocation);

                if (distanceInMeters > courseSetting.ValidateRangeMeters)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = $"Fuera del rango. Distancia: {distanceInMeters:F2}m / Límite: {courseSetting.ValidateRangeMeters}m",
                        Data = null
                    };
                }


                //studentEntry.Status = MessageConstant_Attendance.PRESENT;
                //studentEntry.IsCheckedIn = true;

                var docCacheKey = $"attendance_active_{courseId}";
                var activeDocAttendance = _cache.Get<ActiveAttendanceCacheDto>(docCacheKey);

                if (activeDocAttendance != null)
                {
                    var docStudentEntry = activeDocAttendance.Students
                        .FirstOrDefault(s => s.Email == email);

                    if (docStudentEntry != null)
                    {
                        docStudentEntry.Status = MessageConstant_Attendance.PRESENT;
                        docStudentEntry.Attendend = true;
                    }

                    // Reescribir el cache del docente actualizado
                    _cache.Set(docCacheKey, activeDocAttendance, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = activeDocAttendance.Expiration
                    });
                }

                // Registrar asistencia en BD
                var attendance = new AttendanceEntity
                {
                    CourseId = courseId,
                    StudentId = student.Id,
                    Attended = true,
                    Status = MessageConstant_Attendance.PRESENT,
                    RegistrationDate = DateTime.UtcNow,
                    Method = Attendance_Helpers.TYPE_OTP,
                    CreatedBy = activeAttendance.UserId,
                    ChangeBy = Attendance_Helpers.STUDENT,
                    CreatedDate = DateTime.UtcNow,
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesWithoutAuditAsync();
                ////////////////////////////////////////////////////////////////////////////////////
                activeAttendance.Entries.Remove(studentEntry);
                _logger.CreateLogger($"Estudiante eliminado del grupo {courseId}. Quedan {activeAttendance.Entries.Count} estudiantes");

                // Notificar cambio de estado
                await _hubContext.Clients.Group(courseId.ToString())
                    .SendAsync(Attendance_Helpers.UPDATE_ATTENDANCE_STATUS, new
                    {
                        studentId = student.Id,
                        status = MessageConstant_Attendance.PRESENT
                    });

                // Retornar éxito
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "Asistencia registrada exitosamente.",
                    Data = new StudentAttendanceResponse
                    {
                        FullName = $"{student.FirstName} {student.LastName}",
                        CourseId = courseId,
                        Distance = distanceInMeters,
                        Method = Attendance_Helpers.TYPE_OTP,
                        Status = MessageConstant_Attendance.PRESENT,
                        Email = student.Email,
                        CourseName = courseName
                        

                    }
                };
            }
            catch (Exception ex)
            {
                var logger = _logger.CreateLogger<AttendanceRSignalService>();
                
                logger.LogError(ex, "Error al registrar asistencia por OTP");
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = "Error interno al procesar la asistencia.",
                    Data = null
                };
            }
        }
        /// <summary>
        /// Calcula la distancia en metros entre dos puntos geográficos usando la fórmula de Haversine.
        /// </summary>
        /// <param name="point1">Primer punto (coordenadas WGS84). X=Longitud, Y=Latitud</param>
        /// <param name="point2">Segundo punto (coordenadas WGS84). X=Longitud, Y=Latitud</param>
        /// <returns>Distancia en metros entre los puntos</returns>
        /// <remarks>
        /// Implementación basada en la fórmula de Haversine para cálculo de distancias en esferas.
        ///     Precisión: ~99.5% para distancias cortas/moderadas (menos de 500km).
        /// Referencias:
        ///     <seealso href="https://www.neovasolutions.com/2019/10/04/haversine-vs-vincenty-which-is-the-best/"/>
        ///     <see href="https://en.wikipedia.org/wiki/Haversine_formula"/>
        ///     <see href="https://www.movable-type.co.uk/scripts/latlong.html"/>
        ///     <see href="https://www.movable-type.co.uk/scripts/latlong.html"/>
        ///     <see href="https://stackoverflow.com/questions/55092618/gps-is-the-haversine-formula-accurate-for-distance-between-two-nearby-gps-poin"/>
        ///     <see href="https://forum.arduino.cc/t/fasthaversine-an-approximation-of-haversine-for-short-distances/324628/5"/>
        /// </remarks>
        static double CalculateHaversineDistance(Point point1, Point point2)
        {
            const double EarthRadiusMeters = 6_371_000; 
            var lat1 = point1.Y * Math.PI / 180.0;
            var lon1 = point1.X * Math.PI / 180.0;
            var lat2 = point2.Y * Math.PI / 180.0;
            var lon2 = point2.X * Math.PI / 180.0;

            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }

        /// <summary>
        /// Valida un código OTP contra una clave secreta con manejo de intentos fallidos
        /// </summary>
        /// <param name="secretKey">Clave secreta generada para el usuario</param>
        /// <param name="otp">Código OTP a validar</param>
        /// <returns>True si el OTP es válido, False en caso contrario</returns>
        private async Task<bool> ValidateOtpAsync(string secretKey, string otp, string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentNullException(nameof(secretKey));

            if (string.IsNullOrWhiteSpace(otp))
                return false;

            bool isValid = ValidateOtp(secretKey, otp);

            if (!isValid)
                return false;

            _cache.Remove(cacheKey);


            return true;
        }
        public bool ValidateOtp(string secretKey, string otpToValidate, int otpExpirationSeconds = 60)
        {
            var totp = new Totp(Base32Encoding.ToBytes(secretKey), step: otpExpirationSeconds);

            // Permite un margen de error de ±1 intervalo 
            return totp.VerifyTotp(otpToValidate, out long timeStepMatched, new VerificationWindow(previous: 1, future: 1));
        }

        public Task<ResponseDto<object>> SendAttendanceByQr(string Email, float x, float y, string MAC, Guid courseId)
        {
            throw new NotImplementedException();
        }
    }
}
