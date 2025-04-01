
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Services.Distance;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Constants;
using System.Collections.Concurrent;
using System.Net;
using ClassNotes.API.Services.Audit;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace ClassNotes.API.Services
{
    public class EmailAttendanceService : IEmailAttendanceService
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Factoría para crear ámbitos de inyección de dependencias (IServiceScope).
        /// Permite resolver servicios con un ciclo de vida controlado manualmente.
        /// Evita problemas con servicios scoped que podrían ser compartidos incorrectamente
        /// entre diferentes hilos de ejecución.
        /// Mejora el rendimiento al asignar varios hilos para mandar el correos y no usar el mismo canal o hilo
        /// De esta manera se evita Altos tiempos de espera 
        /// </summary>
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ClassNotesContext _context;
       // private readonly IEmailsService _emailsService;
        private readonly ILogger<EmailAttendanceService> _logger;
        private readonly DistanceService _distanceService;
        private readonly IAttendancesService _attendanceService;
        private readonly OTPCleanupService _otpCleanupService;


        /// <summary>
        /// Proporciona acceso al Hub de SignalR para notificaciones en tiempo real.
        /// Permite enviar actualizaciones a clientes conectados sobre:
        /// - Progreso de operaciones
        /// - Finalización de tareas
        /// - Eventos importantes
        /// </summary>
        private readonly IHubContext<AttendanceHub> _hubContext;
       // private readonly EmailScheduleService _emailScheduleService;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Controla el número máximo de operaciones concurrentes permitidas.
        /// Se implementó para prevenir sobrecarga del servidor cuando muchas tareas
        /// intentan ejecutarse simultáneamente. El límite actual es de 7 tareas concurrentes.
        /// 
        /// Funcionamiento:
        /// - Cada tarea debe "adquirir" el semáforo antes de ejecutarse
        /// - Si ya hay 7 tareas ejecutándose, nuevas tareas esperarán
        /// - Cuando una tarea finaliza, libera un espacio para otra
        /// 
        /// NOTAS: Si se implementa un segundo servidor se tendra que hacer una modulacion 
        /// entre los 2 servidores de correos para mejorar el rendimiento de cargas
        /// </summary>
        private SemaphoreSlim _semaphore; // Límite de tareas concurrentes

        // Lista privada para almacenar los OTPs
        private static readonly ConcurrentDictionary<string, StudentOTPDto> _otpDictionary = new();

        public EmailAttendanceService(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ClassNotesContext context,
        //    IEmailsService emailsService,
            ILogger<EmailAttendanceService> logger,
            DistanceService distanceService,
            IAttendancesService attendanceService,
            OTPCleanupService otpCleanupService,
            IHubContext<AttendanceHub> hubContext,
           // EmailScheduleService emailScheduleService,
            IAuditService auditService,
            IMapper mapper
            )
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _context = context;
           // _emailsService = emailsService;
            _logger = logger;
            _distanceService = distanceService;
            _attendanceService = attendanceService;
            _otpCleanupService = otpCleanupService;
            _hubContext = hubContext;
          //  _emailScheduleService = emailScheduleService;
            _auditService = auditService;
            _mapper = mapper;

            // Configuracion para saber la cantidad de cuentas SMTP disponibles
            var smtpAccounts = configuration.GetSection("SmtpAccounts").Get<List<SMTPAcountDto>>()
                 ?? new List<SMTPAcountDto>();
            //Por cada cuenta asignamos 7 hilos para no saturar el servidor
            var maxConcurrent = smtpAccounts.Count * 7;           
            _semaphore = new SemaphoreSlim(maxConcurrent);

        }

        /// <summary>
        /// Envía correos electrónicos con códigos OTP a todos los estudiantes de una clase para validar asistencia
        /// </summary>
        /// <param name="request">DTO con los parámetros de envío (ID clase, ubicación, configuración OTP)</param>
        /// <returns>ResponseDto con lista de estados de envío para cada estudiante</returns>
        /// <exception cref="ArgumentException">
        /// Se lanza cuando:
        /// - El usuario no tiene permisos para la clase
        /// - No existen estudiantes en la clase
        /// - Ningún estudiante tiene email válido
        /// </exception>
        /// <remarks>
        /// Flujo del proceso:
        /// 1. Valida parámetros de entrada
        /// 2. Obtiene la clase con sus estudiantes
        /// 3. Filtra estudiantes con email válido
        /// 4. Envía correos en paralelo con OTPs
        /// 5. Programa envíos automáticos si está configurado
        /// 6. Retorna resultados de todos los envíos
        /// </remarks>
        public async Task<ResponseDto<List<SendEmailsStatusDto>>> SendEmailsAsync(EmailAttendanceRequestDto request)
        {

            var userId = _auditService.GetUserId();
            var students = await _context.Courses
                    .Where(sc => sc.Id == request.ClaseId &&
                                 sc.Center.TeacherId == userId)
                    .SelectMany(c => c.Students // Accedemos a la relación de StudentCourses
                        .Where(sc => sc.IsActive && sc.CourseId == request.ClaseId) // Filtramos solo los estudiantes activos
                        .Select(sc => sc.Student)) // Traemos solo los datos del estudiante
                    .ProjectTo<StudentSenEmailDto>(_mapper.ConfigurationProvider)
                    .AsNoTracking()
                    .ToListAsync();
            var courseInformation = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId);

            var course = await _context.Courses
                .Include(c => c.CourseSetting) 
                .FirstOrDefaultAsync();

            var expiredTime = course?.CourseSetting.MinimumAttendanceTime ?? 30;


            if (students is null)
            {
                return new ResponseDto<List<SendEmailsStatusDto>>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = $"{MessagesConstant.RECORD_NOT_FOUND}",
                    Data = null
                };
            }
            if (request.RangoValidacionMetros < 30 || request.RangoValidacionMetros > 100)
            {
                return new ResponseDto<List<SendEmailsStatusDto>>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = $"{MessagesConstant.ATT_INVALID_RADIUS}",
                    Data = null
                };
            }
            
            if (!students.Any()) return new ResponseDto<List<SendEmailsStatusDto>>
            {
                StatusCode = 404, 
                Status = false,
                Message = $"{ MessagesConstant.STU_RECORDS_NOT_FOUND}",
                Data = null
            };


            // PREPARACIÓN PARA ENVÍO MASIVO
            // Colección thread-safe para almacenar resultados de cada envío
            var emailStatuses = new ConcurrentBag<SendEmailsStatusDto>();

            // PROCESAMIENTO EN PARALELO
            // Crea una tarea por cada estudiante para enviar correos concurrentemente
            var tasks = students.Select(estudiante => 
                ProcessEmailAsync(
                    estudiante,
                    courseInformation,
                    request,
                    emailStatuses,
                    userId,
                    expiredTime
                    ));
            await Task.WhenAll(tasks);

            SendActiveOTPsToCleanupService();

            //if (request.EnvioAutomatico)
            //{
            //    _emailScheduleService.AddOrUpdateConfig(
            //        request.ClaseId,
            //        true,
            //        TimeSpan.Parse(request.HoraEnvio),
            //        request.DiasEnvio.Select(d => (DayOfWeek)d).ToList()
            //    );
            //}
            SendActiveOTPsToCleanupService();

            // CONFIGURACIÓN DE ENVÍOS AUTOMÁTICOS
            // Si está activado el envío automático, programa futuros envíos
            //if (request.EnvioAutomatico)
            //{
            //    _emailScheduleService.AddOrUpdateConfig(
            //        request.ClaseId,
            //        true,
            //        TimeSpan.Parse(request.HoraEnvio),
            //        request.DiasEnvio.Select(d => (DayOfWeek)d).ToList()
            //    );
            //}

            return new ResponseDto<List<SendEmailsStatusDto>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.EMAIL_SENT_SUCCESSFULLY,
                Data = emailStatuses.ToList()
            };
        }

        /// <summary>
        /// Procesa el envío de correo electrónico a un estudiante con su OTP para validar asistencia
        /// </summary>
        /// <param name="estudiante">Entidad del estudiante con sus datos personales</param>
        /// <param name="clase">Entidad del curso al que pertenece el estudiante</param>
        /// <param name="request">DTO con los parámetros de la solicitud de asistencia</param>
        /// <param name="emailStatuses">Colección thread-safe para registrar el estado de envío</param>
        /// 
        /// <returns>Tarea asíncrona que representa la operación de envío</returns>
        /// 
        /// <remarks>
        /// Este método realiza las siguientes operaciones:
        /// 1. Genera un OTP único para el estudiante
        /// 2. Crea y registra la información del OTP
        /// 3. Prepara el correo electrónico con el OTP
        /// 4. Envía el correo usando un semáforo para control de concurrencia
        /// 5. Registra el resultado en la colección de estados
        /// </remarks>
        private async Task ProcessEmailAsync(
            StudentSenEmailDto estudiante,
            CourseEntity clase,
            EmailAttendanceRequestDto request,
            ConcurrentBag<SendEmailsStatusDto> emailStatuses,
            string userId,
            int expiredTime
            )
        {
            var otp = GenerateOTP();

            
            var studentOTP = new StudentOTPDto
            {
                OTP = otp,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                StudentId = estudiante.Id,
                StudentName = estudiante.FirstName,
                StudentEmail = estudiante.Email,
                CourseId = clase.Id,
                TeacherId = userId,
                ExpirationDate = DateTime.UtcNow.AddMinutes(expiredTime),
                RangoValidacionMetros = request.RangoValidacionMetros
            };

            AddOTP(studentOTP);

            var emailDto = CreateEmailDto(estudiante, clase, otp, expiredTime);


            
            using var scope = _scopeFactory.CreateScope();

            // Obtiene una instancia del servicio de correos dentro del scope creado
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailsService>();

            
            await SendEmailWithSemaphoreAsync(emailDto, estudiante.Email, estudiante.FirstName, otp, scopedEmailService);

             // Registro del resultado exitoso
            emailStatuses.Add(new SendEmailsStatusDto
            {
                StudentName = estudiante.FirstName,
                Email = estudiante.Email,
                OTP = otp,
                SentStatus = true,
                Message = "Correo enviado correctamente."
            });
        }

        private EmailDto CreateEmailDto(StudentSenEmailDto estudiante, CourseEntity clase, string otp, int tiempoExpiracion)
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

        private async Task SendEmailWithSemaphoreAsync(EmailDto emailDto, string email, string studentName, string otp, IEmailsService scopedEmailService)
        {
            await _semaphore.WaitAsync();
            try
            {
                var result = await scopedEmailService.SendEmailAsync(emailDto);

                if (result.Status)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveEmailSent", new
                    {
                        StudentName = studentName,
                        Email = email,
                        OTP = otp
                    });
                }
                else
                {
                    _logger.LogError($"Error al enviar email a {email}: {result.Message}");

                }
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /// <summary>
        /// Valida la asistencia del estudiante utilizando OTP y verificación de geolocalización.
        /// </summary>
        /// <param name="request">Contiene los datos de validación incluyendo el OTP y las coordenadas actuales</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        /// <exception cref="ArgumentException">
        /// Se lanza cuando:
        /// - El OTP es inválido o ha expirado
        /// - El estudiante está fuera del radio permitido
        /// - No se encuentra el registro del estudiante
        /// </exception>
        /// 
        /// <remarks>
        /// Este método realiza las siguientes operaciones en secuencia:
        /// 1. Validación del OTP (existencia y vigencia)
        /// 2. Verificación de geolocalización (dentro del radio permitido)
        /// 3. Verificación del registro del estudiante
        /// 4. Ejecución en paralelo de:
        ///    - Creación del registro de asistencia
        ///    - Notificación en tiempo real via SignalR
        /// 5. Limpieza del OTP
        /// </remarks>
        public async Task<ResponseDto<AttendanceResultDto>> ValidateAttendanceAsync(ValidateAttendanceRequestDto request)
        {
            // TryGetValue es thread-safe y más eficiente que FirstOrDefault -> Bajar tiempos de carga
            if (!_otpDictionary.TryGetValue(request.OTP, out var studentOTP) || studentOTP.ExpirationDate < DateTime.UtcNow)
            {
                return new ResponseDto<AttendanceResultDto>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Status = false,
                    Message = MessagesConstant.OTP_EXPIRED_OR_INVALID,
                    Data = null
                };
            }

            // Validación de ubicación
            // registrada cuando se generó el OTP usando el servicio de geolocalización
            var distance = _distanceService.CalcularDistancia(
                request.Latitude, request.Longitude, 
                studentOTP.Latitude, studentOTP.Longitude);

            if (distance > studentOTP.RangoValidacionMetros)
            {
                return new ResponseDto<AttendanceResultDto>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Status = false,
                    Message = string.Format(MessagesConstant.ATT_INVALID_RADIUS, studentOTP.RangoValidacionMetros),
                    Data = null
                };
            }

            // Consulta optimizada a la base de datos:
            // - Usa AsNoTracking porque solo necesitamos leer datos
            // - Solo Campos Necesarios
            var estudiante = await _context.Students
                .Where(s => s.Id == studentOTP.StudentId) //Ver donde el ID coinciden
                .Select(s => new { s.FirstName, s.Email, s.Id }) // seleciona el Primer Nombre, Email, Id
                .AsNoTracking() // Solo leer
                .FirstOrDefaultAsync(); 

            if (estudiante == null)
            {
                return new ResponseDto<AttendanceResultDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Status = false,
                    Message = MessagesConstant.ATT_STUDENT_NOT_ENROLLED,
                    Data = null
                };
            }

            // Preparación del registro de asistencia
            // Crea el DTO con los datos necesarios para registrar la asistencia
            var attendanceCreateDto = new AttendanceCreateDto
            {
                Attended = true, // Marca asistencia como positiva
                Status = $"{MessageConstant_Attendance.PRESENT}", //presente
                CourseId = studentOTP.CourseId,
                StudentId = studentOTP.StudentId,
                TeacherId = studentOTP.TeacherId,
            };

            // Procesamiento en paralelo
            // Ejecuta dos operaciones asíncronas simultáneamente:
            // 1. Crear el registro de asistencia en la base de datos
            // 2. Notificar a todos los clientes conectados via SignalR
            var tasks = new[]
            {
                _attendanceService.CreateAttendanceAsync(attendanceCreateDto),
                _hubContext.Clients.All.SendAsync("ReceiveAttendanceValidation", new
                {
                    StudentName = estudiante.FirstName,
                    estudiante.Email,
                    AttendanceStatus = $"{MessageConstant_Attendance.PRESENT}",
                    ValidationTime = DateTime.UtcNow
                })
            };
            // Espera a que ambas tareas completen
            await Task.WhenAll(tasks);

            // Limpieza final
            // Elimina el OTP ya utilizado para evitar reutilización
            RemoveOTP(studentOTP);
            // 6. Respuesta exitosa
            return new ResponseDto<AttendanceResultDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Status = true,
                Message = MessagesConstant.ATT_UPDATE_SUCCESS,
                Data = new AttendanceResultDto
                {
                    StudentName = estudiante.FirstName,
                    CourseId = studentOTP.CourseId,
                }
            };
        }
        /// <summary>
        /// Agrega un nuevo OTP al diccionario concurrente.
        /// </summary>
        /// <param name="otp">DTO que contiene la información del OTP generado para el estudiante</param>
        /// <remarks>
        /// - Utiliza TryAdd para asegurar operación atómica y thread-safe
        /// - Si el OTP ya existe, no lo sobrescribe (retorna false silenciosamente)
        /// - La clave del diccionario es el código OTP que es unico
        /// </remarks>
        public void AddOTP(StudentOTPDto otp)
        {
            _otpDictionary.TryAdd(otp.OTP, otp);
        }
        /// <summary>
        /// Obtiene todos los OTPs que han expirado según la fecha/hora actual (UTC).
        /// </summary>
        /// <returns>Lista de OTPs expirados</returns>
        /// <remarks>
        /// - Filtra los OTPs cuya ExpirationDate es menor a DateTime.UtcNow (que ya vencio) 
        /// - Convertimos a lista para evitar problemas de enumeración modificable
        /// </remarks>
        public List<StudentOTPDto> GetExpiredOTPs()
        {
            return _otpDictionary.Values.Where(otp => otp.ExpirationDate < DateTime.UtcNow).ToList();
        }

        /// <summary>
        /// Obtiene todos los OTPs que aún son válidos (no han expirado).
        /// </summary>
        /// <returns>Lista de OTPs activos</returns>
        /// <remarks>
        /// - Filtra los OTPs cuya ExpirationDate es mayor o igual a DateTime.UtcNow
        /// - Convertimos a lista para materializar los resultados
        /// </remarks>
        public List<StudentOTPDto> GetActiveOTPs()
        {
            return _otpDictionary.Values.Where(otp => otp.ExpirationDate >= DateTime.UtcNow).ToList();
        }
        /// <summary>
        /// Elimina un OTP específico del diccionario.
        /// </summary>
        /// <param name="otp">DTO del OTP a eliminar</param>
        /// <remarks>
        /// - Usa TryRemove para operación thread-safe
        /// - El parámetro out _ descarta el valor removido (no lo necesitamos)
        /// - No lanza excepción si el OTP no existe (tarda mas en cargar ademas qeq
        /// no se ha resportado caso donde un otp no existe esta ligado a los estudiantes basicamente
        /// </remarks>
        public void RemoveOTP(StudentOTPDto otp)
        {
            _otpDictionary.TryRemove(otp.OTP, out _);
        }

        // Método para generar un OTP
        public string GenerateOTP()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Método para enviar la lista de OTPs activos a OTPCleanupService
        public void SendActiveOTPsToCleanupService()
        {
            var activeOTPs = GetActiveOTPs();
            _otpCleanupService.ReceiveActiveOTPs(activeOTPs);
        }
    }
}
