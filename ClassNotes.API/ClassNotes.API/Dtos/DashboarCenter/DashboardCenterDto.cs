namespace ClassNotes.API.Dtos.DashboarCenter
{
    public class DashboardCenterDto
    {
        public DashboarCenterSummaryDto Summary { get; set; }
        public List<DashboarCenterActiveClassDto> ActiveClasses { get; set; }
    }
}
