using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Notes;
using ClassNotes.API.Dtos.Notes.QualificationDasboard;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Services.Notes
{
    public interface INotesService
    {
        Task<ResponseDto<PaginationDto<List<StudentActivityNoteDto>>>> GetStudentsActivitiesAsync(Guid courseId, int page = 1);
        Task<ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>> GetStudentsNotesAsync(Guid courseId, int page = 1);
        Task<ResponseDto<PaginationDto<List<StudentUnitNoteDto>>>> GetStudentUnitsNotesAsync(Guid studentId, Guid courseId, int page = 1);

        Task<ResponseDto<DasboardRequestDto>> GetDashboardQualifications(
            Guid courseId,
            string activeStudent = null,
            string studentStateNote = null,
            bool includeStats = true,
            int page = 1,
            int pageSize = 10,
            string SearchTerm = ""
        );

    }
}
