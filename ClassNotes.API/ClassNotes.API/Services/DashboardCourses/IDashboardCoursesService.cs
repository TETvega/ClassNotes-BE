using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboardCourses;

namespace ClassNotes.API.Services.DashboardCourses
{
    // --------------------- CP --------------------- //
    public interface IDashboardCoursesService
    {
        Task<ResponseDto<DashboardCourseDto>> GetDashboardCourseAsync(Guid courseId); // Para ver el dashboard del curso
    }
}