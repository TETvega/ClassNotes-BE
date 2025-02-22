using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.Centers
{
    public interface ICentersService
    {
        Task<ResponseDto<CenterDto>> ArchiveAsync(Guid id);
        Task<ResponseDto<CenterDto>> CreateAsync(CenterCreateDto dto);
        Task<ResponseDto<CenterDto>> DeleteAsync(bool confirmation, Guid id);
        Task<ResponseDto<CenterDto>> EditAsync(CenterEditDto dto, Guid id);
        Task<ResponseDto<CenterDto>> GetCenterByIdAsync(Guid id);
        Task<ResponseDto<PaginationDto<List<CenterDto>>>> GetCentersListAsync(string searchTerm = "", bool isArchived = false, int page = 1);
    }
}
