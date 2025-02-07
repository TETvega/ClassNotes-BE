using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Students;

namespace ClassNotes.API.Services.Students
{
	public interface IStudentsService
	{
        Task<ResponseDto<StudentDto>> CreateStudentAsync(StudentCreateDto studentCreateDto);
        Task<ResponseDto<StudentDto>> DeleteStudentAsync(Guid id);
        Task<ResponseDto<PaginationDto<List<StudentDto>>>> GetStudentsListAsync(string searchTerm = "", int page = 1);
        Task<ResponseDto<StudentDto>> UpdateStudentAsync(Guid id, StudentEditDto studentEditDto);
    }
}
