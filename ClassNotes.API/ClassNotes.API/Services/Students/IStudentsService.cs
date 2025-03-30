using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Students;

namespace ClassNotes.API.Services.Students
{
	public interface IStudentsService
	{
        Task<ResponseDto<PaginationDto<List<StudentDto>>>> GetStudentsListAsync(string searchTerm = "", int page = 1);
        Task<ResponseDto<StudentDto>> GetStudentByIdAsync(Guid id);
        Task<ResponseDto<StudentResultDto>> CreateStudentAsync(StudentCreateDto studentCreateDto, bool strictMode);
        Task<ResponseDto<StudentDto>> UpdateStudentAsync(Guid id, StudentEditDto studentEditDto);
        Task<ResponseDto<List<Guid>>> DeleteStudentsInBatchAsync(List<Guid> studentIds);

    }
}
