using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;

namespace ClassNotes.API.Services.Courses
{
        public interface ICoursesService
        {
        // CP -> Listar todos los cursos 	
        Task<ResponseDto<PaginationDto<List<CourseWithSettingDto>>>> GetCoursesListAsync(
                string searchTerm = "", int page = 1 , int? pageSize = null
        );

        // CP -> Listar un curso en especifico
        Task<ResponseDto<CourseWithSettingDto>> GetCourseByIdAsync(Guid id);

        // CP -> Crear un curso 
        Task<ResponseDto<CourseWithSettingDto>> CreateAsync (CourseWithSettingCreateDto dto);

        // CP -> Editar cursos 
        Task<ResponseDto<CourseDto>> EditAsync(CourseEditDto dto, Guid id);

        // CP -> Eliminar un curso
        Task<ResponseDto<CourseWithSettingDto>> DeleteAsync(Guid id);
    }
}
