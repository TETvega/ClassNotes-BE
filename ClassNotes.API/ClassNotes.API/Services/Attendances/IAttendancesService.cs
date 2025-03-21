using ClassNotes.API.Dtos.Attendances;

namespace ClassNotes.API.Services.Attendances
{
    public interface IAttendancesService
    {
        Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto attendanceCreateDto);
        Task<AttendanceDto> EditAttendanceAsync(Guid attendanceId, AttendanceEditDto attendanceEditDto);
        Task<List<AttendanceDto>> ListAttendancesAsync();
        Task<List<AttendanceDto>> ListAttendancesByCourseAsync(Guid courseId);
        Task<List<AttendanceDto>> ListAttendancesByStudentAsync(Guid studentId);
    }
}
