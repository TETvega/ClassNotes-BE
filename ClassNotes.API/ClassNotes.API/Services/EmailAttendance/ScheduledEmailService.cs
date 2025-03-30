using ClassNotes.API.Dtos.EmailsAttendace;
using ClassNotes.API.Services;

public class ScheduledEmailSender : BackgroundService
{
    private readonly ILogger<ScheduledEmailSender> _logger;
    private readonly EmailScheduleService _emailScheduleService;
    private readonly IServiceProvider _serviceProvider;

    public ScheduledEmailSender(
        ILogger<ScheduledEmailSender> logger,
        EmailScheduleService emailScheduleService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _emailScheduleService = emailScheduleService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de envío automático de correos iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ahora = DateTime.UtcNow.TimeOfDay;
                var diaActual = DateTime.UtcNow.DayOfWeek;

                // Recorrer todas las configuraciones de envío automático
                foreach (var config in _emailScheduleService.GetAllConfigs())
                {
                    // Sumar 6 horas a la hora de envío configurada (ajuste intencional)
                    var horaEnvioConDesplazamiento = config.HoraEnvio.Add(TimeSpan.FromHours(6));

                    // Verificar si es el momento de enviar correos
                    if (config.EnvioAutomatico &&
                        config.DiasEnvio.Contains(diaActual) &&
                        horaEnvioConDesplazamiento.Hours == ahora.Hours &&
                        horaEnvioConDesplazamiento.Minutes == ahora.Minutes)
                    {
                        _logger.LogInformation($"Es hora de enviar correos para el curso {config.CourseId}.");

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var emailAttendanceService = scope.ServiceProvider.GetRequiredService<IEmailAttendanceService>();

                            // Ejecutar la acción de enviar correos
                            await emailAttendanceService.SendEmailsAsync(new EmailAttendanceRequestDto
                            {
                                ProfesorId = "profesorId", // Reemplaza con el ID del profesor
                                CentroId = config.CourseId, // Usamos el CourseId como CentroId (ajusta según tu lógica)
                                ClaseId = config.CourseId,
                                Latitude = 0, // Ajusta según sea necesario
                                Longitude = 0,
                                RangoValidacionMetros = 100, // Ajusta según sea necesario
                                TiempoExpiracionOTPMinutos = 10, // Ajusta según sea necesario
                                EnvioAutomatico = true,
                                HoraEnvio = config.HoraEnvio.ToString(),
                                DiasEnvio = config.DiasEnvio // Aquí ya es List<DayOfWeek>, no se necesita conversión
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el servicio de envío automático de correos.");
            }

            // Esperar 1 minuto antes de la siguiente verificación
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}