using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.AttendacesRealTime.ForStudents;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Hubs;
using ClassNotes.API.Models;
using ClassNotes.API.Services.Audit;
using ClassNotes.API.Services.ConcurrentGroups;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Services.Otp;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtpNet;
using QRCoder;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Point = NetTopologySuite.Geometries.Point;


namespace ClassNotes.API.Services.AttendanceRealTime
{
    public class AttendanceRSignalService : IAttendanceRSignalService
    {
        private readonly ClassNotesContext _context;
        private readonly IAuditService _auditService;
        private readonly IHubContext<AttendanceHub> _hubContext;
        private readonly IEmailsService _emailsService;
        private readonly IOtpService _otpService;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AttendanceRSignalService> _logger;
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
            ILogger<AttendanceRSignalService> logger,
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
        /// <summary>
        /// Inicia un proceso de asistencia para un curso, generando los mecanismos de autenticación configurados (QR y/o OTP por email).
        /// </summary>
        /// <remarks>
        /// <para>Este endpoint gestiona el flujo completo de inicio de asistencia:</para>
        /// 
        /// <list type="number">
        ///     <item>
        ///         <term>Validaciones iniciales</term>
        ///         <description>
        ///             Verifica que el curso exista, esté activo y pertenezca al docente solicitante.
        ///             Confirma que se haya seleccionado al menos un método de asistencia (QR o Email).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Configuración geográfica</term>
        ///         <description>
        ///             Establece el área geográfica de validación, usando la ubicación predeterminada del curso
        ///             o una nueva proporcionada en la solicitud.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Generación de credenciales</term>
        ///         <description>
        ///             Crea los códigos QR (en formato Base64) y/o OTP (para envío por email)
        ///             según la configuración solicitada.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Almacenamiento temporal</term>
        ///         <description>
        ///             Guarda en caché distribuida la información de la sesión de asistencia con
        ///             un tiempo de expiración configurable.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Notificaciones</term>
        ///         <description>
        ///             Emite eventos via SignalR para actualizar el estado en clientes conectados.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <para><strong>Referencias técnicas:</strong></para>
        /// <list type="bullet">
        ///     <item><see href="https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory">Memoria Caching en ASP.NET Core</see></item>
        ///     <item><see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions">Opciones de caché</see></item>
        ///     <item><see href="https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext">SignalR HubContext</see></item>
        /// </list>
        /// </remarks>
        /// <param name="request">
        /// <see cref="AttendanceRequestDto"/> que contiene:
        /// <list type="bullet">
        ///     <item><description>CourseId: Identificador único del curso</description></item>
        ///     <item><description>AttendanceType: Configuración de métodos de asistencia</description></item>
        ///     <item><description>StrictMode: Habilita validación estricta con MAC address</description></item>
        ///     <item><description>NewGeolocation: Opcional - Nueva ubicación para validación</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <see cref="ResponseDto{AttendanceResponseDto}"/> con:
        /// <list type="bullet">
        ///     <item><description>StatusCode: Código HTTP de resultado</description></item>
        ///     <item><description>Data: Información detallada de la sesión iniciada</description></item>
        ///     <item><description>Message: Mensaje descriptivo del resultado</description></item>
        /// </list>
        /// </returns>
        /// <response code="200">Proceso completado correctamente. Retorna datos de la sesión creada.</response>
        /// <response code="400">
        ///     Error en parámetros de entrada. Puede ser por:
        ///     <list type="bullet">
        ///         <item><description>Ningún método de asistencia seleccionado</description></item>
        ///         <item><description>Configuración geográfica inválida</description></item>
        ///     </list>
        /// </response>
        /// <response code="404">Curso no encontrado o no accesible para el usuario.</response>
        /// <response code="500">Error interno del servidor al procesar la solicitud.</response>
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
                 .Include(x => x.CourseSetting)
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
            
            //Se confirma que en el tiempo minimo mas  al menos 5 minutos no sea un nuevo dia...
            if (DateTimeOffset.Now.AddMinutes(5 + course.CourseSetting.MinimumAttendanceTime).Date > DateTimeOffset.Now.Date)
            {
                return new ResponseDto<object>
                {
                    StatusCode = 405,
                    Status = false,
                    Message = "No se puede ingresar una asistencia muy cerca de el dia siguiente, al menos 7 minutos de diferencia.",
                    Data = null
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
            if (request.StrictMode && request.AttendanceType.Qr && request.AttendanceType.Email)
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

                locationToUse = _mapper.Map<Point>(request.NewGeolocation);
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
            if (courseSetting.MinimumAttendanceTime < 5)
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
                qrContent = $"{course.Id}|{request.StrictMode}|{expiration}";

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
                ExpirationTime = expiration.AddSeconds(20),
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
                    // await _emailsService.SendEmailAsync(emailDto);
                }

                var memoryEntry = new TemporaryAttendanceEntry
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    Otp = otpCode,
                    QrContent = qrContent,
                    ExpirationTime = expiration,
                    Email = student.Email,
                    GeolocationLatitud = (float)(locationToUse?.Y ?? 0f),
                    GeolocationLongitud = (float)(locationToUse?.X ?? 0f),
                    StudentFirstName = student.FirstName,
                    StudentLastName = student.LastName,
                    
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
                    course.Section,
                    minimumAttendanceTime = course.CourseSetting.MinimumAttendanceTime+2

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
        /// Almacena en caché distribuida los datos de asistencia de un estudiante para un curso específico,
        /// implementando un sistema de expiración automática con registro de ausencias.
        /// </summary>
        /// <remarks>
        /// <para>Este método proporciona un sistema completo de gestión temporal de asistencias con:</para>
        ///
        /// <list type="bullet">
        ///     <item>
        ///         <term>Almacenamiento temporal</term>
        ///         <description>
        ///             Guarda en memoria los datos de asistencia pendiente usando el formato de clave:
        ///             <c>"{studentId}_{courseId}"</c>
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Mecanismo de expiración automática</term>
        ///         <description>
        ///             Implementa un callback que se ejecuta al expirar el registro para:
        ///             <list type="number">
        ///                 <item>Verificar si el estudiante no marcó asistencia</item>
        ///                 <item>Registrar automáticamente como "AUSENTE" en base de datos</item>
        ///                 <item>Notificar a todos los clientes suscritos al grupo SignalR del curso</item>
        ///             </list>
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Sistema de notificaciones</term>
        ///         <description>
        ///             Emite eventos via SignalR utilizando <see cref="IHubContext{T}"/> para:
        ///             <list type="bullet">
        ///                 <item>Actualizaciones en tiempo real del estado de asistencia</item>
        ///                 <item>Sincronización entre múltiples dispositivos</item>
        ///             </list>
        ///         </description>
        ///     </item>
        /// </list>
        /// <para><strong>Referencias técnicas:</strong></para>
        /// <list type="bullet">
        ///     <item><see href="https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed">Caché distribuida en ASP.NET Core</see></item>
        ///     <item><see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions">MemoryCacheEntryOptions</see></item>
        ///     <item><see href="https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext">SignalR HubContext</see></item>
        /// </list>
        /// </remarks>
        /// <param name="userId">
        /// Identificador del usuario docente que realiza la operación.
        /// <para>Usado para:</para>
        /// <list type="bullet">
        ///     <item>Auditoría de registros</item>
        ///     <item>Validación de permisos</item>
        ///     <item>Registro de cambios</item>
        /// </list>
        /// </param>
        /// <param name="student">
        /// Entidad completa del estudiante con:
        /// <list type="bullet">
        ///     <item>Datos personales</item>
        ///     <item>Relación con el curso</item>
        ///     <item>Estado académico</item>
        /// </list>
        /// </param>
        /// <param name="courseId">
        /// Identificador único del curso relacionado.
        /// <para>Formato: GUID</para>
        /// </param>
        /// <param name="memoryEntry">
        /// Datos temporales de asistencia a almacenar, incluyendo:
        /// <list type="bullet">
        ///     <item>Estado actual (WAITING/PRESENT/ABSENT)</item>
        ///     <item>Método de registro (QR/OTP)</item>
        ///     <item>Datos geolocalización</item>
        ///     <item>Timestamp de creación</item>
        /// </list>
        /// </param>
        /// <param name="expiration">
        /// Fecha/hora exacta de expiración del registro.
        /// <para>Determina:</para>
        /// <list type="bullet">
        ///     <item>Duración máxima de la sesión de asistencia</item>
        ///     <item>Momento para el callback de expiración</item>
        /// </list>
        /// </param>
        /// <returns>
        /// Operación asíncrona sin retorno de valor.
        /// <para>Efectos secundarios:</para>
        /// <list type="bullet">
        ///     <item>Entrada almacenada en caché distribuida</item>
        ///     <item>Callback registrado para manejo de expiración</item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Cuando alguno de los parámetros requeridos es nulo.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Cuando los formatos de ID son inválidos.
        /// </exception>
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
            var method = attendanceType.Email && attendanceType.Qr ? Attendance_Helpers.TYPE_BOUGTH
                       : attendanceType.Email ? Attendance_Helpers.TYPE_OTP
                       : Attendance_Helpers.TYPE_QR;

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
            float y,
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

                var activeCacheKey = $"attendance_active_{courseId}";
                var activeCacheAttendance = _cache.Get<ActiveAttendanceCacheDto>(activeCacheKey);

                // Validar que el metodo OTP este permitido
                if (activeCacheAttendance == null ||
                    (activeCacheAttendance.AttendanceMethod != Attendance_Helpers.TYPE_OTP &&
                     activeCacheAttendance.AttendanceMethod != Attendance_Helpers.TYPE_BOUGTH))
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "Metodo de asistencia via OTP no esta habilitado para esta sesion.",
                        Data = null
                    };
                }

                if (OTP == "" || OTP == null )
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 404,
                        Status = false,
                        Message = "No se ha Ingresado un Codigo OTP",
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
                        Message = "Estudiante no encontrado o no esta inscrito en el curso.",
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
                        Message = "El estudiante no esta registrado en la lista de asistencia o ya ha sido marcado.",
                        Data = null
                    };
                }

                if (studentEntry.Otp != OTP)
                {
                    return new ResponseDto<StudentAttendanceResponse>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "OTP invalido o expirado.",
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
                _logger.LogInformation($"Estudiante eliminado del grupo {courseId}. Quedan {activeAttendance.Entries.Count} estudiantes");

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
                

                _logger.LogError(ex, "Error al registrar asistencia por OTP");
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
        /// DEPRECADO 
        /// Valida un código OTP contra una clave secreta 
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

        /// <summary>
        /// Registra la asistencia de un estudiante mediante código QR, validando múltiples factores de seguridad.
        /// </summary>
        /// <remarks>
        /// Este método realiza las siguientes operaciones:
        /// 1. Valida que exista una sesión de asistencia activa para el curso
        /// 2. Verifica que el estudiante esté matriculado en el curso
        /// 3. Comprueba que el estudiante esté en estado "waiting" (en caché)
        /// 4. Valida la ubicación geográfica dentro del rango permitido
        /// 5. En modo estricto, verifica y registra la dirección MAC del dispositivo
        /// 6. Actualiza todos los registros de asistencia
        /// </remarks>
        /// <param name="courseId">Identificador único del curso</param>
        /// <param name="Email">Correo electrónico del estudiante</param>
        /// <param name="x">Coordenada X (longitud) de la ubicación del estudiante</param>
        /// <param name="y">Coordenada Y (latitud) de la ubicación del estudiante</param>
        /// <param name="MAC">Dirección MAC del dispositivo (requerida solo en modo estricto)</param>
        /// <returns>
        /// Objeto ResponseDto con:
        /// - StatusCode: 200 si es exitoso, otros códigos para errores
        /// - Status: booleano indicando éxito/fracaso
        /// - Message: Mensaje descriptivo del resultado
        /// - Data: Detalles de la asistencia registrada
        /// </returns>
        public async Task<ResponseDto<StudentAttendanceResponse>> SendAttendanceByQr(
            Guid courseId,
            string Email,
            float x,
            float y,
            string MAC = "")
        {
            var activeCacheKey = $"attendance_active_{courseId}";
            var activeCacheAttendance = _cache.Get<ActiveAttendanceCacheDto>(activeCacheKey);

            // Validar que el metodo QR este permitido
            if (activeCacheAttendance == null ||
                (activeCacheAttendance.AttendanceMethod != Attendance_Helpers.TYPE_QR &&
                 activeCacheAttendance.AttendanceMethod != Attendance_Helpers.TYPE_BOUGTH))
            {
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "El metodo QR no este habilitado para esta sesion de asistencia",
                    Data = null
                };
            }
            // Validacion de el grupo de asistencia 
            var activeAttendance = _groupCacheManager.GetGroupCache(courseId);
            var validationResult = ValidateActiveAttendance(activeAttendance);
            if (validationResult != null) return validationResult;

            // Validacion de pertenecia al curso
            var (student, courseName, studentValidationResult) = await GetAndValidateStudent(courseId, Email);
            if (studentValidationResult != null) return studentValidationResult;

            // validacion con la cache  
            // validacion que el estidante este es estado weating (este en la cache todavia)
            var studentEntry = _groupCacheManager.TryGetStudentEntryByEmail(courseId, Email);
            var entryValidationResult = ValidateStudentEntry(studentEntry);
            if (entryValidationResult != null) return entryValidationResult;

            // Validaciones respecto a distancias
            // esta dentro de los parametrso del curso en si 
            var locationValidationResult = await ValidateLocation(studentEntry, x, y);
            if (locationValidationResult.Message != null) return locationValidationResult;

            // Validar modo estricto (solo verificación, sin registro 
            var docCacheKey = $"attendance_active_{courseId}";
            var activeDocAttendance = _cache.Get<ActiveAttendanceCacheDto>(docCacheKey);
            var macValidationResult = await CheckMacAddressRequirements(activeDocAttendance, MAC);
            if (macValidationResult != null) return macValidationResult;

            // Actualizar registros (incluye registro de MAC si esta en modo estricto )
            var updateResult = await UpdateAttendanceRecordsWithMac(
                activeDocAttendance,
                Email,
                courseId,
                student,
                activeAttendance,
                MAC);

            if (updateResult != null) return updateResult;

            // Retorno de la respuesta 
            return new ResponseDto<StudentAttendanceResponse>
            {
                StatusCode = 200,
                Status = true,
                Message = "Asistencia registrada exitosamente.",
                Data = new StudentAttendanceResponse
                {
                    FullName = $"{student.FirstName} {student.LastName}",
                    CourseId = courseId,
                    Distance = locationValidationResult.Data.Distance,
                    Method = Attendance_Helpers.TYPE_QR,
                    Status = MessageConstant_Attendance.PRESENT,
                    Email = student.Email,
                    CourseName = courseName
                }
            };
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        // Metdos separados de ayuda 

        private ResponseDto<StudentAttendanceResponse> ValidateActiveAttendance(AttendanceGroupCache activeAttendance)
        {
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
            return null;
        }

        private async Task<(StudentEntity student, string courseName, ResponseDto<StudentAttendanceResponse> validationResult)>
            GetAndValidateStudent(Guid courseId, string email)
        {
            var student = await _context.Students
                .Include(s => s.Courses.Where(sc => sc.CourseId == courseId && sc.IsActive))
                .ThenInclude(sc => sc.Course)
                .FirstOrDefaultAsync(s => s.Email == email);

            var courseName = student?.Courses.FirstOrDefault()?.Course?.Name ?? "No hay nombre";

            if (student == null)
            {
                return (null, null, new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = "Estudiante no encontrado o no está inscrito en el curso.",
                    Data = null
                });
            }

            return (student, courseName, null);
        }

        private ResponseDto<StudentAttendanceResponse> ValidateStudentEntry(TemporaryAttendanceEntry studentEntry)
        {
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
            return null;
        }

        private async Task<ResponseDto<StudentAttendanceResponse>> ValidateLocation(TemporaryAttendanceEntry studentEntry, float x, float y)
        {
            var cachedLocation = new Point(studentEntry.GeolocationLongitud, studentEntry.GeolocationLatitud) { SRID = 4326 };
            var receivedLocation = new Point(x, y) { SRID = 4326 };

            var course = await _context.Courses
                .Include(c => c.CourseSetting)
                .FirstOrDefaultAsync(c => c.Id == studentEntry.CourseId);

            if (course?.CourseSetting == null)
            {
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "No se encontró configuración de asistencia para este curso.",
                    Data = null
                };
            }

            double distanceInMeters = CalculateHaversineDistance(cachedLocation, receivedLocation);

            if (distanceInMeters > course.CourseSetting.ValidateRangeMeters)
            {
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = $"Fuera del rango. Distancia: {distanceInMeters:F2}m / Límite: {course.CourseSetting.ValidateRangeMeters}m",
                    Data = null
                };
            }

            // Creamos un Objeto para poder mandar la respuesta de la distacioa
            // esto para que si podamos mandar cualquiera de las validaciones o el objeto de distancia
            return new ResponseDto<StudentAttendanceResponse>
            {
                Data = new StudentAttendanceResponse { Distance = distanceInMeters }
            };
        }

        // Nuevo método solo para verificar requisitos MAC (sin registro)
        /// <summary>
        ///     Verifica los requisitos de dirección MAC para el registro de asistencia en modo estricto.
        ///     Las validaciones de MAC se Utilizo  para generar el REGEX 
        /// </summary>
        /// <remarks>
        ///     Este método realiza las siguientes validaciones:
        ///     <list type="number">
        ///         <item><description>Verifica que exista una sesión de asistencia activa</description></item>
        ///         <item><description>En modo estricto, valida que la dirección MAC tenga el formato correcto</description></item>
        ///     </list>
        /// 
        ///     El formato válido para direcciones MAC debe seguir los estándares IEEE 802:
        ///         <see href="https://standards.ieee.org/wp-content/uploads/import/documents/tutorials/macgrp.pdf"/>
        ///         <see href="https://chatgpt.com/share/68017c8c-4f34-8007-b1c9-5a1d08533cf5"/>
        ///     Formatos aceptados: XX:XX:XX:XX:XX:XX o XX-XX-XX-XX-XX-XX (hexadecimal)
        /// </remarks>
        ///     <param name="activeDocAttendance">Datos de la sesión de asistencia activa</param>
        ///     <param name="MAC">Dirección MAC proporcionada por el dispositivo</param>
        /// <returns>
        ///     <para><see cref="ResponseDto{StudentAttendanceResponse}"/> con error si:</para>
        ///     <list type="bullet">
        ///     <item><description>No hay sesión activa (400)</description></item>
        ///     <item><description>MAC es requerida pero no válida (400)</description></item>
        ///     </list>
        ///     <para>Null si todas las validaciones son exitosas</para>
        /// </returns>
        /// <example>
        /// Ejemplo de respuesta con error:
        /// <code>
        /// {
        ///   StatusCode: 400,
        ///   Status: false,
        ///   Message: "Se requiere una dirección MAC válida...",
        ///   Data: null
        /// }
        /// </code>
        /// </example>
        private async Task<ResponseDto<StudentAttendanceResponse>> CheckMacAddressRequirements(
            ActiveAttendanceCacheDto activeDocAttendance,
            string MAC)
        {
            _logger.BeginScope(nameof(CheckMacAddressRequirements),  activeDocAttendance.Students.ToArray());
            if (activeDocAttendance == null)
            {
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Ya no hay mas registros por validar",
                    Data = null
                };
            }

            // para obtener in registro de tipo mac

            if (activeDocAttendance.StrictMode &&
                (string.IsNullOrWhiteSpace(MAC) || !Regex.IsMatch(MAC, @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")))
            {
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Se requiere una dirección MAC válida en formato XX:XX:XX:XX:XX:XX o XX-XX-XX-XX-XX-XX",
                    Data = null
                };
            }

            return null;
        }

        private async Task<ResponseDto<StudentAttendanceResponse>> UpdateAttendanceRecordsWithMac(
                ActiveAttendanceCacheDto activeDocAttendance,
                string email,
                Guid courseId,
                StudentEntity student,
                AttendanceGroupCache activeAttendance,
                string MAC)
        {
            try
            {
                _logger.BeginScope(nameof(UpdateAttendanceRecordsWithMac));
                //  Verificar MAC solo si estamos en modo estricto
                if (activeDocAttendance?.StrictMode == true && !string.IsNullOrEmpty(MAC))
                {
                    var macControlKey = $"mac_global_{courseId}";
                    var macDictionary = _cache.Get<Dictionary<string, Guid>>(macControlKey) ?? new Dictionary<string, Guid>();

                    if (macDictionary.ContainsKey(MAC))
                    {
                        return new ResponseDto<StudentAttendanceResponse>
                        {
                            StatusCode = 400,
                            Status = false,
                            Message = "Este dispositivo ya ha registrado una asistencia en este curso.",
                            Data = null
                        };
                    }
                }

                //  Actualizar cache del docente
                if (activeDocAttendance != null)
                {
                    var docStudentEntry = activeDocAttendance.Students.FirstOrDefault(s => s.Email == email);
                    if (docStudentEntry != null)
                    {
                        docStudentEntry.Status = MessageConstant_Attendance.PRESENT;
                        docStudentEntry.Attendend = true;
                    }

                    _cache.Set($"attendance_active_{courseId}", activeDocAttendance, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = activeDocAttendance.Expiration
                    });
                }

                //  Registrar en base de datos
                var attendance = new AttendanceEntity
                {
                    CourseId = courseId,
                    StudentId = student.Id,
                    Attended = true,
                    Status = MessageConstant_Attendance.PRESENT,
                    RegistrationDate = DateTime.UtcNow,
                    Method = Attendance_Helpers.TYPE_QR,
                    CreatedBy = activeAttendance.UserId,
                    ChangeBy = Attendance_Helpers.STUDENT,
                    CreatedDate = DateTime.UtcNow,
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesWithoutAuditAsync();
                _logger.LogInformation($"[ QR_Service ] : Registro en la Base de Datos de {attendance}");

                //  Registrar MAC SOLO despus de confirmar el registro en BD
                if (activeDocAttendance?.StrictMode == true && !string.IsNullOrEmpty(MAC))
                {
                    
                    _logger.LogInformation("[ UPDATE_ATTENDANCE_QR ] : Modo Stricto -> registrando MAC");
                    var macControlKey = $"mac_global_{courseId}";
                    var macDictionary = _cache.Get<Dictionary<string, Guid>>(macControlKey) ?? new Dictionary<string, Guid>();
                    macDictionary[MAC] = student.Id;



                    _cache.Set(macControlKey, macDictionary, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = activeDocAttendance.Expiration
                    });
                }

                // Eliminar de la cache del grupo
                activeAttendance.Entries.RemoveAll(e => e.Email == email);
                _logger.LogInformation($"Estudiante eliminado del grupo {courseId}. Quedan {activeAttendance.Entries.Count} estudiantes");

                // Notificar cambio de estado
                await _hubContext.Clients.Group(courseId.ToString())
                    .SendAsync(Attendance_Helpers.UPDATE_ATTENDANCE_STATUS, new
                    {
                        studentId = student.Id,
                        status = MessageConstant_Attendance.PRESENT
                    });

                return null; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar registros de asistencia");
                return new ResponseDto<StudentAttendanceResponse>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = "Error interno al registrar la asistencia",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Obtiene el registro de asistencias de estudiantes para un curso específico,
        /// implementando una estrategia de Cache-First con fallback a base de datos.
        /// 
        /// Cuando hay datos en caché (memoria), indica que el proceso de asistencia está activo.
        /// Cuando se consulta desde base de datos, indica que el proceso ya finalizó.
        /// </summary>
        /// <param name="courseId">Identificador único del curso</param>
        /// <returns>
        /// Respuesta paginada que contiene:
        /// - Lista de estudiantes con su estado de asistencia
        /// - Metadatos de paginación
        /// - Estado de la operación
        /// </returns>
        /// <remarks>
        /// Comportamiento según origen de datos:
        /// 
        /// [DATOS DESDE CACHÉ (Memoria)]:
        /// - Indica que el proceso de asistencia está en curso
        /// - Los campos pueden contener valores temporales/no definitivos
        /// - Estudiantes sin registrar mostrarán:
        ///   - attendanceEntryDatee: Fecha actual (DateTime.Now)
        ///   - isCheckedIn: false
        ///   - status: "WAITING"
        ///   - attendanceMethod: null
        ///   - changeBy: null
        /// 
        /// [DATOS DESDE BASE DE DATOS]:
        /// - Indica que el proceso de asistencia finalizó
        /// - Todos los valores son definitivos
        /// - Estudiantes sin asistencia registrada mostrarán:
        ///   - attendanceEntryDatee: DateTime.MinValue
        ///   - isCheckedIn: false
        ///   - status: "NOT_PRESENT"
        ///   - attendanceMethod: "MANUALLY" (valor por defecto)
        ///   - changeBy: "SYSTEM" (valor por defecto)
        /// </remarks>
        public async Task<ResponseDto<PaginationDto<List<StudentsAttendanceEntries>>>> GetStudentsAttendancesToday(Guid courseId)
        {
            var userId = _auditService.GetUserId();

            // Verificar si el usuario es dueño del curso
            var isOwner = await _context.Courses
                .Where(c => c.Center.TeacherId == userId)
                .AnyAsync(c => c.Id == courseId);

            if (!isOwner)
            {
                return new ResponseDto<PaginationDto<List<StudentsAttendanceEntries>>>()
                {
                    Status = false,
                    StatusCode = 404,
                    Message = "No es el dueño del curso",
                    Data = null
                };
            }

            // Intentar obtener datos de la cache
            var activeAttendanceCache = _groupCacheManager.GetGroupCache(courseId);
            List<StudentsAttendanceEntries> attendanceEntries;

            if (activeAttendanceCache != null && activeAttendanceCache.Entries.Any())
            {
                _logger.LogInformation("[ GET_CACHE ] : Registros desde la Cache");
                // Si hay datos en cache, mapearlos al DTO
                attendanceEntries = activeAttendanceCache.Entries.Select(entry =>
                    new StudentsAttendanceEntries
                    {
                        StudentId = entry.StudentId,
                        StudentName = entry.StudentFirstName ?? "Desconocido", // Obtener de cache si est disponible
                        StdentLastName = entry.StudentLastName ?? "Desconocido", // Obtener de cache si est disponible
                        Email = entry.Email,
                        AttendanceEntryDatee = entry.AttendanceEntry ?? DateTime.Now, // fecha actual , no se manda de cache por el momento
                        IsCheckedIn = entry.IsCheckedIn,
                        Status = entry.Status,
                        AttendanceMethod = entry.AttendanceMethod,
                        ChangeBy = entry.ChangeBy ?? null
                    }).ToList();
            }
            else
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                _logger.LogInformation("[ GET_BD ]: get desde la Base de datos");
                _logger.LogWarning($"[ GET_BD ] : Fecha para filtrar será {today}");

                var studentsInCourse = await _context.StudentsCourses
                    .Where(sc => sc.CourseId == courseId && sc.IsActive)
                    .Select(sc => new
                    {
                        StudentId = sc.Student.Id,
                        StudentName = sc.Student.FirstName,
                        StudentLastName = sc.Student.LastName,
                        Email = sc.Student.Email,
                        Attendance = sc.Student.Attendances
                            .Where(a => a.CourseId == courseId &&
                                        a.RegistrationDate >= today &&
                                        a.RegistrationDate < tomorrow)
                            .OrderByDescending(a => a.RegistrationDate)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Validar si ningun estudiante tiene asistencia registrada hoy
                bool TodayPassAttendnace = studentsInCourse.All(s => s.Attendance == null);

                if (TodayPassAttendnace)
                {
                    _logger.LogInformation("[ GET_BD ]: No se ha pasado asistencia -> se retorna lista []");
                    return new ResponseDto<PaginationDto<List<StudentsAttendanceEntries>>>()
                    {
                        Status = true,
                        StatusCode = 200,
                        Message = "No Hay Asistencias Vigentes este Dia",
                        Data = null
                    };
                }
                _logger.LogInformation($" [ BD_EXCECUTION ] : {studentsInCourse.Count()}");
                attendanceEntries = studentsInCourse.Select(s =>
                    new StudentsAttendanceEntries
                    {
                        StudentId = s.StudentId,
                        StudentName = s.StudentName,
                        StdentLastName = s.StudentLastName,
                        Email = s.Email,
                        AttendanceEntryDatee = s.Attendance?.RegistrationDate ?? DateTime.MinValue,
                        IsCheckedIn = s.Attendance?.Attended ?? false,
                        Status = s.Attendance?.Status ?? MessageConstant_Attendance.NOT_PRESENT,
                        AttendanceMethod = s.Attendance?.Method ?? Attendance_Helpers.TYPE_MANUALLY,
                        ChangeBy = s.Attendance?.ChangeBy ?? Attendance_Helpers.SYSTEM
                    }).ToList();
            }
                // Paginar resultados 
                var paginatedResponse = new PaginationDto<List<StudentsAttendanceEntries>>
                {
                    Items = attendanceEntries,
                    TotalItems = attendanceEntries.Count,
                    CurrentPage = 1,
                    PageSize = 1,
                    HasNextPage = false,
                    HasPreviousPage = false,
                    TotalPages = 1
                };
                return new ResponseDto<PaginationDto<List<StudentsAttendanceEntries>>>()
                {
                    Status = true,
                    StatusCode = 200,
                    Message = "Datos de asistencia obtenidos correctamente",
                    Data = paginatedResponse
                };
        }
    }
}
