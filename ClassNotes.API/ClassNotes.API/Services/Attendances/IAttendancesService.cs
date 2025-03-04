using ClassNotes.API.Dtos.Attendances;

namespace ClassNotes.API.Services.Attendances
{
	public interface IAttendancesService
	{
        Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto attendanceCreateDto);
    }
}
