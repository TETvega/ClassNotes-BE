using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.Students;

namespace ClassNotes.API.Services.Students
{
	public interface IStudentsService
	{
        Task<ResponseDto<PaginationDto<List<StudentDto>>>> GetStudentsListAsync(string searchTerm = "", int? pageSize = null ,int page = 1);
        Task<ResponseDto<StudentDto>> GetStudentByIdAsync(Guid id);

        Task<ResponseDto<List<PendingClassesDto>>>   GetPendingActivitiesClasesListAsync(Guid id, int? top = null  );
        Task<ResponseDto<StudentResultDto>> CreateStudentAsync(BulkStudentCreateDto bulkStudentCreateDto);
        Task<ResponseDto<StudentDto>> UpdateStudentAsync(Guid id, StudentEditDto studentEditDto);
        Task<ResponseDto<List<Guid>>> DeleteStudentsInBatchAsync(List<Guid> studentIds, Guid courseId);
        Task<ResponseDto<PaginationDto<List<StudentPendingDto>>>> GetAllStudentsPendingActivitiesAsync(Guid id, string searchTerm = "", int? pageSize = null, int page = 1, string StudentType = "All", string ActivityType = "All");
        Task<ResponseDto<PaginationDto<List<ActivityDto>>>> GetStudentPendingActivitiesAsync(Guid id, int? pageSize = null, int page = 1);
        Task<ResponseDto<List<StudentDto>>> ReadExcelFileAsync(IFormFile file, bool strictMode);
    }
}
