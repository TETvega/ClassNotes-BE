using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.Centers
{
    public interface ICentersService
    {
        Task<ResponseDto<CenterDto>> CreateAsync(CenterCreateDto dto);
        Task<ResponseDto<CenterDto>> EditAsync(CenterEditDto dto, Guid id);
    }
}
