using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Services.Centers
{
    public interface ICentersService
    {
        Task<ResponseDto<CenterDto>> ArchiveAsync(Guid id);
        Task<ResponseDto<CenterDto>> CreateAsync([FromForm] CenterCreateDto dto, IFormFile image);
        Task<ResponseDto<CenterDto>> DeleteAsync(bool confirmation, Guid id);
        Task<ResponseDto<CenterDto>> EditAsync([FromForm] CenterEditDto dto, Guid id, IFormFile image, bool changedImage);
        Task<ResponseDto<CenterDto>> GetCenterByIdAsync(Guid id);
        Task<ResponseDto<PaginationDto<List<CenterDto>>>> GetCentersListAsync(string searchTerm = "", bool isArchived = false, int page = 1);
        Task<ResponseDto<CenterDto>> RecoverAsync(Guid id);
    }
}
