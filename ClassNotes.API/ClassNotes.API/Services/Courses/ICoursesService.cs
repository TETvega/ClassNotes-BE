using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;

namespace ClassNotes.API.Services.Courses
{
	public interface ICoursesService
	{
		// CP -> Listar un curso en especifico (por nombre)
		Task<ResponseDto<CourseDto>> GetCourseByNameAsync(string name);

		// CP -> Eliminar un curso
		Task<ResponseDto<CourseDto>> DeleteAsync(Guid id);
	}
}
