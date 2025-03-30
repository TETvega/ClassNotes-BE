using System;
using System.Collections.Generic;

public class EmailScheduleService
{
    // Diccionario para almacenar la configuración de envío automático
    private readonly Dictionary<Guid, EmailScheduleConfig> _configs = new();

    // Método para agregar o actualizar la configuración
    public void AddOrUpdateConfig(Guid courseId, bool envioAutomatico, TimeSpan horaEnvio, List<DayOfWeek> diasEnvio)
    {
        _configs[courseId] = new EmailScheduleConfig
        {
            CourseId = courseId,
            EnvioAutomatico = envioAutomatico,
            HoraEnvio = horaEnvio,
            DiasEnvio = diasEnvio
        };
    }

    // Método para obtener la configuración de un curso
    public EmailScheduleConfig GetConfig(Guid courseId)
    {
        return _configs.TryGetValue(courseId, out var config) ? config : null;
    }

    // Método para obtener todas las configuraciones
    public IEnumerable<EmailScheduleConfig> GetAllConfigs()
    {
        return _configs.Values;
    }

    // Método para eliminar la configuración de un curso
    public void RemoveConfig(Guid courseId)
    {
        _configs.Remove(courseId);
    }
}

// Clase para representar la configuración de envío automático
public class EmailScheduleConfig
{
    public Guid CourseId { get; set; } // Guid
    public bool EnvioAutomatico { get; set; }
    public TimeSpan HoraEnvio { get; set; } // TimeSpan
    public List<DayOfWeek> DiasEnvio { get; set; }
}