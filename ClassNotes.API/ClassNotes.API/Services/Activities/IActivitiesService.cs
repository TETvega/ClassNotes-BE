using Azure;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Students;

namespace ClassNotes.API.Services.Activities
{
	// --------------------- CP --------------------- //
	public interface IActivitiesService
	{
        // Listar todas las actividades
        Task<ResponseDto<PaginationDto<List<ActivitySummaryDto>>>> GetActivitiesListAsync(
            string searchTerm = "",
            int page = 1,
            int? pageSize = 10,
            Guid? centerId = null,
            Guid? tagActivityId = null,
            string typeActivities = "ALL"
        );

        Task<ResponseDto<PaginationDto<List<ActivityResponseDto>>>> GetAllActivitiesByClassAsync(
            Guid id,
            string searchTerm = "",
            int page = 1,
            int? pageSize = 10,
            Guid? tagActivityId = null,
            Guid? unitId = null,
            string typeActivities = "ALL",
            string isExtraFilter = "ALL"
        );

        // Listar una actividad en especifico
        Task<ResponseDto<ActivityDto>> GetActivityByIdAsync(Guid id);

		// Crear una actividad
		Task<ResponseDto<ActivityDto>> CreateAsync(ActivityCreateDto dto);

		// Editar una actividad
		Task<ResponseDto<ActivityDto>> EditAsync(ActivityEditDto dto, Guid id);

		// Eliminar una actividad
		Task<ResponseDto<ActivityDto>> DeleteAsync(Guid id);

        Task<ResponseDto<List<StudentActivityNoteDto>>> ReviewActivityAsync(List<StudentActivityNoteCreateDto> dto, Guid ActivityId);
        Task<ResponseDto<PaginationDto<List<StudentAndNoteDto>>>> GetStudentsActivityScoreAsync(Guid activityId, int page = 1);
    }
}
