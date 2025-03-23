using System;
using ClassNotes.API.Services.Date;


public class DateTimeService : IDateTimeService
{
    public TimeSpan GetCurrentTime()
    {
        return DateTime.UtcNow.TimeOfDay; // Hora actual en formato HH:mm:ss
    }

    public DayOfWeek GetCurrentDayOfWeek()
    {
        return DateTime.UtcNow.DayOfWeek; // Día de la semana actual
    }
}