using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;

namespace ClassNotes.API.Services.Courses
{
	public interface ICoursesService
	{
		// EG -> Listar todos los cursos 
		Task<ResponseDto<PaginationDto<List<CourseDto>>>> GetCoursesListAsync(
	   string searchTerm = "", int page = 1);

        // CP -> Listar un curso en especifico (por nombre)
       Task<ResponseDto<CourseDto>> GetCourseByNameAsync(string name);

		// EG -> Editar cursos 
        

        // CP -> Eliminar un curso
        Task<ResponseDto<CourseDto>> DeleteAsync(Guid id);
	}
}
