using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;

namespace ClassNotes.API.Services.Courses
{
        public interface ICoursesService
        {
        // EG -> Listar todos los cursos 	
        Task<ResponseDto<PaginationDto<List<CourseDto>>>> GetCoursesListAsync(
                string searchTerm = "", int page = 1 , int? pageSize = null
        );

        // CP -> Listar un curso en especifico
        Task<ResponseDto<CourseDto>> GetCourseByIdAsync(Guid id);

        // CP -> Crear un curso 
        Task<ResponseDto<CourseDto>> CreateAsync (CourseCreateDto dto);

        // EG -> Editar cursos 
        Task<ResponseDto<CourseDto>> EditAsync(CourseEditDto dto, Guid id);

        // CP -> Eliminar un curso
        Task<ResponseDto<CourseDto>> DeleteAsync(Guid id);
        }
}
