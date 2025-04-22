using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Attendances.Student;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.TagsActivities;

namespace ClassNotes.API.Services.Attendances
{
    public interface IAttendancesService
	{
		// AM: Obtener stats de las asistencias por Id del curso
		Task<ResponseDto<CourseAttendancesDto>> GetCourseAttendancesStatsAsync(Guid courseId);

		// AM: Mostrar paginación de estudiantes por Id del curso
		Task<ResponseDto<PaginationDto<List<CourseAttendancesStudentDto>>>> GetStudentsAttendancesPaginationAsync(Guid courseId, bool? isActive = null, string searchTerm = "", int page = 1,int? pageSize=null);

        // AM: Obtener stats de las asistencias por estudiante
        Task<ResponseDto<StudentAttendancesDto>> GetStudentAttendancesStatsAsync(StudentIdCourseIdDto dto, bool isCurrentMonth = false);

        // AM: Mostrar paginación de asistencias por estudiante
        Task<ResponseDto<PaginationDto<List<StudentsDATAAttendances>>>> GetAttendancesByStudentPaginationAsync(StudentIdCourseIdDto dto, string searchTerm = "", int page = 1, bool isCurrentMonth = false, int pageSize = 10);
        Task<ResponseDto<AttendanceDto>> SetAttendaceAsync(AttendanceCreateDto dto);
    }
}
