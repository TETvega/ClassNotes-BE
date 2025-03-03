using ClassNotes.API.Dtos.DashboardHome;

namespace ClassNotes.API.Dtos.Dashboard;

public class DashboardHomeDto
{
    public int CentersCount { get; set; }
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
    public List<DashboardHomePendingActivityDto> PendingActivities { get; set; }
    public List<DashboardHomeUpcomingActivityDto> UpcomingActivities { get; set; }
    public List<DashboardHomeCenterDto> Centers { get; set; }
    public List<DashboardHomeActiveCourseDto> ActiveCourses { get; set; }
    public List<DashboardHomeRecentStudentDto> RecentStudents { get; set; }
}
