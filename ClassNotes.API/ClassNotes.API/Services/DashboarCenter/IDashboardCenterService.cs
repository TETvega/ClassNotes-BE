using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboarCenter;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Services.DashboarCenter
{
    public interface IDashboardCenterService
    {
        Task<ResponseDto<DashboardCenterDto>> GetDashboardCenterAsync(
            Guid centerId, string searchTerm = "", int page = 1, int? pageSize = null, string classType = "ACTIVE");
    }
}
