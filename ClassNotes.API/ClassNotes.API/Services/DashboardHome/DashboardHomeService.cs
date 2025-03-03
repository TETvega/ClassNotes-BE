using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Dashboard;
using ClassNotes.API.Services.Audit;

namespace ClassNotes.API.Services.DashboardHome;

public class DashboardHomeService : IDashboardHomeService
{
    private readonly ClassNotesContext _context;
    private readonly ILogger<DashboardHomeService> _logger;
    private readonly IAuditService _auditService;

    public DashboardHomeService(
            ClassNotesContext context,
            ILogger<DashboardHomeService> logger,
            IAuditService auditService
        )
    {
        this._context = context;
        this._logger = logger;
        this._auditService = auditService;
    }

    public async Task<ResponseDto<DashboardHomeDto>> GetDashboardHomeAsync()
    {
        return null;
    }
}
