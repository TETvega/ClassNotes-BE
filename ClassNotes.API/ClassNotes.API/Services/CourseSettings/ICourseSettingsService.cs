using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseSettings;

namespace ClassNotes.API.Services.CoursesSettings
{
    // --------------------- CP --------------------- //
    public interface ICourseSettingsService
	{
		// Listar todas las configuraciones que tiene creadas el docente
		Task<ResponseDto<PaginationDto<List<CourseSettingDto>>>> GetCourseSettingsListAsync(
			string searchTerm = "",
			int page = 1
		);

		// Listar una configuración en especifico
		Task<ResponseDto<CourseSettingDto>> GetCourseSettingByIdAsync(Guid id);

		// Crear una configuración
		Task<ResponseDto<CourseSettingDto>> CreateAsync(CourseSettingCreateDto dto);

		// Editar una configuración
		Task<ResponseDto<CourseSettingDto>> EditAsync(CourseSettingEditDto dto, Guid id);

		// Eliminar una configuración
		Task<ResponseDto<CourseSettingDto>> DeleteAsync(Guid id);
	}
}
