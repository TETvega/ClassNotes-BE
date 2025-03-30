using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Dtos.EmailsAttendace;
using System.Diagnostics;
using ClassNotes.API.Database.Entities;
using static ClassNotes.API.Database.ClassNotesContext;
using ClassNotes.API.Services.Audit;
using static iText.IO.Util.IntHashtable;
using iText.Svg.Renderers.Path.Impl;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Components.Forms;
using System.Drawing;
using ClassNotes.API.Constants;

namespace ClassNotes.API.Services
{
    public class OTPCleanupService : BackgroundService
    {
        private readonly ILogger<OTPCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private bool _messageShown = false;

        private List<StudentOTPDto> _activeOTPs = new List<StudentOTPDto>();

        public OTPCleanupService(
            ILogger<OTPCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void ReactivateService()
        {
            if (!_isRunning)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
                _messageShown = false;
                _logger.LogInformation("Servicio de limpieza de OTPs reactivado.");
            }
            else
            {
                _logger.LogInformation("El servicio de limpieza de OTPs ya está activo.");
            }
        }

        public void ReceiveActiveOTPs(List<StudentOTPDto> activeOTPs)
        {
            _activeOTPs = activeOTPs;
            ReactivateService();
        }
        // HR -- Refactorizacion del Codigo
        // Definicion del Flujo del servicio
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de limpieza de OTPs iniciado.");

            //El servicio entra en un bucle que continuará ejecutándose hasta que reciba una solicitud de cancelación.
            // 
            while (!stoppingToken.IsCancellationRequested)
            {
                //Verifica si el servicio está en condiciones de operar
                //Si no está listo, omite el resto del ciclo y vuelve a verificar en la siguiente iteración.
                if (!await VerificarEstadoDelServicio(stoppingToken))
                    continue;


                await ProcesarOTPsCaducados(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                //El servicio espera 1 minuto antes de la siguiente ejecución.
                //También verifica el token de cancelación durante esta espera.
            }

            _logger.LogInformation("Servicio de limpieza de OTPs detenido.");
        }



        // Verificar si el servicio esta en condiciones de operar 
        // HH
        /// <summary>
        /// Este método verifica si el servicio de limpieza de OTPs debe ejecutarse o no, basándose en dos condiciones principales: el estado de ejecución (_isRunning) y la existencia de OTPs activos.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns>
        /// bool 
        ///   : True = Puede ejecutarse
        ///   : False = No se puede ejecutar
        /// </returns>
        private async Task<bool> VerificarEstadoDelServicio(CancellationToken stoppingToken)
        {
            //*
            //Si el servicio está inactivo (_isRunning == false):
            //Muestra un mensaje de log solo la primera vez (controlado por _messageShown)
            //Espera 1 minuto antes de volver a verificar(para no saturar el sistema con comprobaciones continuas)
            //Retorna false indicando que no debe ejecutarse el procesamiento
            if (!_isRunning)
            {
                if (!_messageShown)
                {
                    _logger.LogInformation("El servicio de limpieza de OTPs está inactivo. Esperando reactivación...");
                    _messageShown = true;
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                return false;
            }

            //Registra en logs cuántos OTPs hay actualmente en la cola de procesamiento
            _logger.LogInformation($"OTPs activos en la lista de espera: {_activeOTPs.Count}.");

            // si no hay 
           // Si no hay OTPs activos:
            //Hace una doble verificación con 30 segundos de espera para evitar falsos negativos
            if (_activeOTPs.Count == 0)
            {
                _logger.LogInformation("No hay OTPs en la lista de espera. Realizando segunda verificación...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                if (_activeOTPs.Count == 0)
                {
                    _logger.LogInformation("Segunda verificación: no hay OTPs activos. Pausando el servicio...");
                    _isRunning = false;
                    return false;
                }
            }

            // si todo lo demas es falso y el servicio esta corriendo y ademas hay OTPs activos
            // entonces esta en condiciones de ejecutarse
            return true;
        }


        //Este método se encarga de procesar los OTPs que han caducado, realizando operaciones de limpieza y registro
        private async Task ProcesarOTPsCaducados(CancellationToken stoppingToken)
        {
           //  Crea un ámbito nuevo para el servicio(para manejar correctamente el ciclo de vida de las dependencias)
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

            //Toma la lista _activeOTPs (OTPs activos)
            //Filtra aquellos cuya fecha de expiración es anterior a la fecha / hora actual(UTC)
            //Convierte a lista para materializar los resultados
            var expiredOTPs = _activeOTPs
                .Where(otp => otp.ExpirationDate < DateTime.UtcNow)
                .ToList();

            // para cada OTP caducato 
            foreach (var otp in expiredOTPs)
            {
                try
                {
                    await CrearAsistenciaSiNoExiste(otp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error procesando OTP para {otp.StudentId}");
                }
                finally
                {
                    _activeOTPs.Remove(otp);
                }
            }
        }

        // Este método se encarga de crear un registro de asistencia para un estudiante en un curso específico, pero solo si no existe previamente un registro de asistencia para esa combinación estudiante-curso.
        private async Task CrearAsistenciaSiNoExiste(StudentOTPDto otp)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            try
            {
                //Busca en la tabla Attendances si ya existe un registro para:
                //El mismo estudiante(StudentId)
                //El mismo curso(CourseId)
                //Donde el campo Attended sea true(asistió)

                var exists = await context.Attendances
                    .AsNoTracking()
                    .AnyAsync(a => a.StudentId == otp.StudentId && a.CourseId == otp.CourseId && a.Attended);
                
                if (!exists)
                {
                    // Referencia al profesor:
                    //Crea una entidad ligera solo con el ID
                    // La "adjunta" al contexto(evita crear un nuevo profesor)
                    var teacher = await context.Users.FirstOrDefaultAsync(u => u.Id == otp.TeacherId);
                    if (teacher == null)
                    {
                        // Maneja el caso en que el usuario no se encuentra
                        _logger.LogError($"Teacher with ID {otp.TeacherId} not found.");
                        return;
                    }
                    var attendance = new AttendanceEntity
                    {
                        Attended = false,
                        Status = $"{MessageConstant_Attendance.NOT_PRESENT}",
                        RegistrationDate = DateTime.UtcNow,
                        CourseId = otp.CourseId,
                        StudentId = otp.StudentId,
                        CreatedByUser = teacher,
                        UpdatedByUser = teacher,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                    };
                    _logger.LogInformation($"Error attendance   {attendance}");

                    await context.Attendances.AddAsync(attendance);
                    await context.SaveChangesWithoutAuditAsync();
                    _logger.LogInformation($"Asistencia creada para {otp.StudentId}");
                }
            }
            finally
            {
                _logger.LogInformation($"Error  {otp }");
            }
        }


    }
}