using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.AttendanceRealTime
{
    public interface IAttendanceRSignalService
    {
        // Servicio para procesar que tipo de asistencia Tomar
        Task<ResponseDto<object>> ProcessAttendanceAsync(AttendanceRequestDto request);


        Task<ResponseDto<object>> SendAttendanceByOtpAsync(string email,  string OTP, float x, float y,Guid courseId);
        Task<ResponseDto<Object>> SendAttendanceByQr(string Email, float x, float y, string MAC , Guid courseId); 
    }
}
