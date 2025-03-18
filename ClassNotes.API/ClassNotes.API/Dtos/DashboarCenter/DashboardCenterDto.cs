using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Dtos.DashboarCenter
{
    public class DashboardCenterDto
    {
        public DashboarCenterSummaryDto Summary { get; set; }
        public PaginationDto<List<DashboarCenterActiveClassDto>> ActiveClasses { get; set; }
    }
}
