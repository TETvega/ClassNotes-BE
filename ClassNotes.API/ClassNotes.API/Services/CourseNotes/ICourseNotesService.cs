using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;

namespace ClassNotes.API.Services.CourseNotes
{
	public interface ICourseNotesService
	{
        Task<ResponseDto<PaginationDto<List<CourseNoteDto>>>> GetAllCourseNotesAsync(FilterCourseNotes dto);

        Task<ResponseDto<List<CoursesNotesDtoViews>>> EditListNotesViews(List<Guid> notesList);
        Task<ResponseDto<CourseNoteDto>> GetCourseNoteByIdAsync(Guid id);
        Task<ResponseDto<CourseNoteDto>> CreateAsync(CourseNoteCreateDto dto);
        Task<ResponseDto<CourseNoteDto>> EditAsync(CourseNoteEditDto dto, Guid id);
        Task<ResponseDto<CourseNoteDto>> DeleteAsync(Guid id);
    }
}
