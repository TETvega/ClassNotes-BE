using ClassNotes.API.Dtos.Allcourses;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseFilter;

namespace ClassNotes.API.Services.AllCourses
{
    public interface ICoursesFilterService
    {
        // Método que obtiene los cursos con paginación y filtros aplicados
        Task<ResponseDto<PaginationDto<List<CourseCenterDto>>>> GetFilteredCourses(CoursesFilterDto filter);
    }
}
