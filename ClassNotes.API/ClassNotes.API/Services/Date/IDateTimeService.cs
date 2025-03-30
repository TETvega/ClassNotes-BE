namespace ClassNotes.API.Services.Date
{
    public interface IDateTimeService
    {
        TimeSpan GetCurrentTime();
        DayOfWeek GetCurrentDayOfWeek();
    }
}
