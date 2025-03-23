using ClassNotes.API.Services.Date;
using Microsoft.AspNetCore.Mvc;
using System;

[ApiController]
[Route("api/[controller]")]
public class DateTimeController : ControllerBase
{
    private readonly IDateTimeService _dateTimeService;

    public DateTimeController(IDateTimeService dateTimeService)
    {
        _dateTimeService = dateTimeService;
    }

    [HttpGet("current-time")]
    public IActionResult GetCurrentTime()
    {
        try
        {
            var currentTime = _dateTimeService.GetCurrentTime();
            return Ok(new { CurrentTime = currentTime.ToString("hh\\:mm\\:ss") });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error al obtener la hora actual.", Error = ex.Message });
        }
    }

    [HttpGet("current-day")]
    public IActionResult GetCurrentDayOfWeek()
    {
        try
        {
            var currentDay = _dateTimeService.GetCurrentDayOfWeek();
            return Ok(new { CurrentDay = currentDay.ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error al obtener el día de la semana actual.", Error = ex.Message });
        }
    }
}