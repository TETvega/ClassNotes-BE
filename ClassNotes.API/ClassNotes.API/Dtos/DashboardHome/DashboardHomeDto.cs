using ClassNotes.API.Dtos.DashboardHome;

namespace ClassNotes.API.Dtos.Dashboard;

public class DashboardHomeDto
{
    public GeneralStadistics Stadistics { get; set; }

    public List<PendingActivities> PendingActivities { get; set; }

    public List<UpcomingActivities> UpcomingActivities { get; set; }

    public List<ActiveCenters> ActiveCenters { get; set; }

    public List<ActiveClasses> ActiveClasses { get; set; }

    public List<StudentPendingActivities> StudentPendingActivitiesList { get; set; }
}
