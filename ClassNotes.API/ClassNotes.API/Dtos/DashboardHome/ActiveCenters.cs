namespace ClassNotes.API.Dtos.DashboardHome
{
    public class ActiveCenters
    {
        public Guid CenterId { get; set; }
        public string CenterName { get; set; }
        public string CenterAbb { get; set; }
        public string LogoUrl { get; set; }
        public int ActiveClasesCount { get; set; }
        public string ActiveStudentsCount { get; set; }
    }
}
