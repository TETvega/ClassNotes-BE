using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;

namespace ClassNotes.API.Services.CourseNotes
{
	public interface ICourseNotesService
	{
        Task<ResponseDto<PaginationDto<List<CourseNoteDto>>>> GetAllCourseNotesAsync(string searchTerm = "", int page = 1);
        Task<ResponseDto<CourseNoteDto>> GetCourseNoteByIdAsync(Guid id);
        Task<ResponseDto<CourseNoteDto>> CreateAsync(CourseNoteCreateDto dto);
        Task<ResponseDto<CourseNoteDto>> EditAsync(CourseNoteEditDto dto, Guid id);
        Task<ResponseDto<CourseNoteDto>> DeleteAsync(Guid id);
    }
}
