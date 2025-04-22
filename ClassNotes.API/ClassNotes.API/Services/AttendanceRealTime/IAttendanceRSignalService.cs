using ClassNotes.API.Dtos.AttendacesRealTime;
using ClassNotes.API.Dtos.AttendacesRealTime.ForStudents;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.AttendanceRealTime
{
    public interface IAttendanceRSignalService
    {
        // Servicio para procesar que tipo de asistencia Tomar
        Task<ResponseDto<object>> ProcessAttendanceAsync(AttendanceRequestDto request);

        Task<ResponseDto<PaginationDto<List<StudentsAttendanceEntries>>>> GetStudentsAttendancesToday(Guid courseId);
        Task<ResponseDto<StudentAttendanceResponse>> SendAttendanceByOtpAsync(string email,  string OTP, float x, float y,Guid courseId);
        Task<ResponseDto<StudentAttendanceResponse>> SendAttendanceByQr(Guid courseId, string Email, float x, float y, string MAC="");
    }
}
