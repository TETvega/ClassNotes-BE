using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.AttendanceRealTime
{
    public interface IAttendanceRSignalService
    {
        // Servicio para procesar que tipo de asistencia Tomar
        Task<ResponseDto<object>> ProcessAttendanceAsync(AttendanceRequestDto request);
    }
}
