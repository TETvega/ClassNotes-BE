using Azure;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;

namespace ClassNotes.API.Services.Activities
{
	// --------------------- CP --------------------- //
	public interface IActivitiesService
	{
		// Listar todas las actividades
		Task<ResponseDto<PaginationDto<List<ActivityDto>>>> GetActivitiesListAsync(
			string searchTerm = "", 
			int page = 1
		);

		// Listar una actividad en especifico
		Task<ResponseDto<ActivityDto>> GetActivityByIdAsync(Guid id);

		// Crear una actividad
		Task<ResponseDto<ActivityDto>> CreateAsync(ActivityCreateDto dto);

		// Editar una actividad
		Task<ResponseDto<ActivityDto>> EditAsync(ActivityEditDto dto, Guid id);

		// Eliminar una actividad
		Task<ResponseDto<ActivityDto>> DeleteAsync(Guid id);

        Task<ResponseDto<StudentActivityNoteDto>> ReviewActivityAsync(StudentActivityNoteCreateDto dto);
    }
}
