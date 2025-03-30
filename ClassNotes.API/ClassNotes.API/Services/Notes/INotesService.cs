using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Services.Notes
{
    public interface INotesService
    {
        Task<ResponseDto<PaginationDto<List<StudentActivityNoteDto>>>> GetStudentsActivitiesAsync(Guid courseId, int page = 1);
        Task<ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>> GetStudentsNotesAsync(Guid courseId, int page = 1);
        Task<ResponseDto<PaginationDto<List<StudentUnitNoteDto>>>> GetStudentUnitsNotesAsync(Guid studentId, Guid courseId, int page = 1);
    }
}
