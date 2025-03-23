using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.TagsActivities;

namespace ClassNotes.API.Services.TagsActivities
{
	public interface ITagsActivitiesService
	{
		// AM: Mostrar todas las etiquetas por paginación
		Task<ResponseDto<PaginationDto<List<TagActivityDto>>>> GetTagsListAsync(string searchTerm = "", int page = 1);

		// AM: Obtener etiqueta por id
		Task<ResponseDto<TagActivityDto>> GetTagByIdAsync(Guid id);

		// AM: Crear una nueva etiqueta
		Task<ResponseDto<TagActivityDto>> CreateTagAsync(TagActivityCreateDto dto);

		// AM: Editar una etiqueta
		Task<ResponseDto<TagActivityDto>> UpdateTagAsync(TagActivityEditDto dto, Guid id);

		// AM: Eliminar una etiqueta
		Task<ResponseDto<TagActivityDto>> DeleteTagAsync(Guid id);

		// AM: Metodo para crear un conjunto de tags predeterminadas
		Task<ResponseDto<List<TagActivityDto>>> CreateDefaultTagsAsync(string userId);
	}
}
