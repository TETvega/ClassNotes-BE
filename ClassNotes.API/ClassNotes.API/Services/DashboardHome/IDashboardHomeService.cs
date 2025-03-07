using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Dashboard;

namespace ClassNotes.API.Services.DashboardHome;

public interface IDashboardHomeService
{
    Task<ResponseDto<DashboardHomeDto>> GetDashboardHomeAsync();
}
