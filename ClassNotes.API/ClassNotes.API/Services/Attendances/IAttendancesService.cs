using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.TagsActivities;

namespace ClassNotes.API.Services.Attendances
{
	public interface IAttendancesService
	{
		// AM: Obtener stats de las asistencias por Id del curso
		Task<ResponseDto<CourseAttendancesDto>> GetCourseAttendancesStatsAsync(Guid courseId);

		// AM: Mostrar paginación de estudiantes por Id del curso
		Task<ResponseDto<PaginationDto<List<CourseAttendancesStudentDto>>>> GetStudentsAttendancesPaginationAsync(Guid courseId, bool? isActive = null, string searchTerm = "", int page = 1);

		// AM: Obtener stats de las asistencias por estudiante
		Task<ResponseDto<StudentAttendancesDto>> GetStudentAttendancesStatsAsync(StudentIdCourseIdDto dto);

		// AM: Mostrar paginación de asistencias por estudiante
		Task<ResponseDto<PaginationDto<List<AttendanceDto>>>> GetAttendancesByStudentPaginationAsync(StudentIdCourseIdDto dto, string searchTerm = "", int page = 1);
	}
}
