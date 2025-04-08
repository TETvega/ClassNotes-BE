using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Dashboard;
using ClassNotes.API.Dtos.DashboardHome;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using System;

namespace ClassNotes.API.Services.DashboardHome;

public class DashboardHomeService : IDashboardHomeService
{
    private readonly ClassNotesContext _context;
    private readonly ILogger<DashboardHomeService> _logger;
    private readonly IAuditService _auditService;
    private readonly IServiceScopeFactory _scopeFactory;

    public DashboardHomeService(
            ClassNotesContext context,
            ILogger<DashboardHomeService> logger,
            IAuditService auditService,
            IServiceScopeFactory scopeFactory
        )
    {
        this._context = context;
        this._logger = logger;
        this._auditService = auditService;
        this._scopeFactory = scopeFactory;
    }
    public async Task<ResponseDto<DashboardHomeDto>> GetDashboardHomeAsync()
    {
        var userId = _auditService.GetUserId();

        // Lanza todas las tareas al mismo tiempo
        var stadisticsTask = GeneralStadisticsAsync(userId);
        var pendingActivitiesTask = GetTopCoursesWithMostPendingActivitiesAsync(userId);
        var upcomingActivitiesTask = GetUpcomingActivitiesAsync(userId);
        var activeCentersTask = GetTopActiveCentersAsync(userId);
        var activeClassesTask = GetActiveClassesAsync(userId);
        var studentsPendingActivitiesTask = GetTopPendingActivitiesStudentAsync(userId);

        // Espera a que todas terminen
        await Task.WhenAll(
            stadisticsTask,
            pendingActivitiesTask,
            upcomingActivitiesTask,
            activeCentersTask,
            activeClassesTask,
            studentsPendingActivitiesTask
        );

        return new ResponseDto<DashboardHomeDto>
        {
            Status = true,
            StatusCode = 200,
            Message = "Exito",
            Data = new DashboardHomeDto
            {
                Stadistics = stadisticsTask.Result,
                PendingActivities = pendingActivitiesTask.Result,
                UpcomingActivities = upcomingActivitiesTask.Result,
                ActiveCenters = activeCentersTask.Result,
                ActiveClasses = activeClassesTask.Result,
                StudentPendingActivitiesList = studentsPendingActivitiesTask.Result
            }
        };
    }
    //public async Task<ResponseDto<DashboardHomeDto>> GetDashboardHomeAsync()
    //{
    //    var userId = _auditService.GetUserId();

    //    var stadistic = GeneralStadisticsAsync( userId ).Result;
    //    var pendingActivities = GetTopCoursesWithMostPendingActivitiesAsync( userId ).Result;

    //    var upcomingActivities = GetUpcomingActivitiesAsync(userId).Result;
    //    var activeCenters = GetTopActiveCentersAsync(userId).Result;
    //    var activeClasses = GetActiveClassesAsync(userId).Result;
    //    var studentsPendingActivities = GetTopPendingActivitiesStudentAsync(userId).Result;

    //    return new ResponseDto<DashboardHomeDto>
    //    {
    //        Status= true,
    //        StatusCode= 200,
    //        Message = "Exito",
    //        Data = new DashboardHomeDto
    //        {
    //            PendingActivities = pendingActivities,
    //            Stadistics = stadistic,
    //            UpcomingActivities = upcomingActivities,
    //            ActiveCenters = activeCenters,
    //            ActiveClasses = activeClasses,
    //            StudentPendingActivitiesList = studentsPendingActivities 
    //        }
    //    };

    //}

    public async Task<List<StudentPendingActivities>> GetTopPendingActivitiesStudentAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

        var result = await context.StudentsCourses
            .AsNoTracking()
            .Where(sc => sc.IsActive &&
                         sc.Course.IsActive &&
                         sc.Course.Center.IsArchived == false &&
                         (sc.Course.CreatedByUser.Id == userId || sc.Course.Center.CreatedByUser.Id == userId))
            .Select(sc => new
            {
                Student = sc.Student,
                sc.Course,
                Units = sc.Course.Units,
                sc.StudentId
            })
            .GroupBy(x => x.Student)
            .Select(group => new StudentPendingActivities
            {
                StudentId = group.Key.Id,
                StudentFullName = group.Key.FirstName+ " " + group.Key.LastName,
                StudentEmail = group.Key.Email,

                StudentActiveClasesCount = group
                    .Select(g => g.Course.Id)
                    .Distinct()
                    .Count(),

                StudentPendingActivitiesCount = group
                    .SelectMany(g => g.Units)
                    .SelectMany(unit => unit.Activities)
                    .Where(activity => activity.QualificationDate < DateTime.UtcNow)
                    .Count(activity =>
                        !activity.StudentNotes
                            .Any(note => note.StudentId == group.Key.Id)
                    )
            })
            .OrderByDescending(s => s.StudentPendingActivitiesCount)
            .Take(10)
            .ToListAsync();

        return result;
    }
    public async Task<List<ActiveClasses>> GetActiveClassesAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var activeClasses = await context.Courses
        .AsNoTracking()
        .Where(course => course.IsActive &&
                         course.Center.IsArchived == false &&
                         (course.CreatedByUser.Id == userId || course.Center.CreatedByUser.Id == userId))
        .Select(course => new ActiveClasses
        {
            CourseId = course.Id,
            CourseName = course.Name,
            CourseCode = course.Code,
            CourseSection = course.Section,

            // Contamos desde la tabla intermedia StudentCourse
            ActiveStudentsCount = course.Students
                .Count(sc => sc.IsActive),

            TotalActivities = course.Units
                .SelectMany(unit => unit.Activities)
                .Count(),

            TotalActivitiesDone = course.Units
                .SelectMany(unit => unit.Activities)
                .Where(activity => activity.QualificationDate < DateTime.UtcNow)
                .Count(activity =>
                    activity.StudentNotes
                        .Select(note => note.StudentId)
                        .Distinct()
                        .Count() >=
                    course.Students
                        .Count(sc => sc.IsActive)
                ),

            CenterId = course.Center.Id,
            CenterName = course.Center.Name,
            CenterAbb = course.Center.Abbreviation
        })
        .OrderBy(c => c.CourseName)
        .Take(4)
        .ToListAsync();

        return activeClasses;
    }

    public async Task<List<ActiveCenters>> GetTopActiveCentersAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var centers = await context.Centers
        .AsNoTracking()
        .Where(c => !c.IsArchived && c.CreatedByUser.Id == userId) // solo centros activos creados por el usuario
        .Select(c => new ActiveCenters
        {
            CenterId = c.Id,
            CenterName = c.Name,
            CenterAbb = c.Abbreviation,
            LogoUrl = c.Logo,

            ActiveClasesCount = c.Courses.Count(course => course.IsActive),

            ActiveStudentsCount = c.Courses
                .Where(course => course.IsActive)
                .SelectMany(course => course.Students)
                .Select(student => student.Id)
                .Distinct()
                .Count()
                .ToString()
        })
        .OrderBy(c => c.CenterName)
        .Take(3)
        .ToListAsync();

        return centers;
    }

    public async Task<List<UpcomingActivities>> GetUpcomingActivitiesAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var currentDate = DateTime.UtcNow;

        var result = await context.Activities
            .AsNoTracking()
            .Where(a => a.QualificationDate > currentDate) // solo futuras
            .Where(a => a.Unit.Course.IsActive) // clases activas
            .Where(a => !a.Unit.Course.Center.IsArchived && a.Unit.Course.Center.TeacherId == userId) // centros válidos
            .OrderBy(a => a.QualificationDate) // próximas primero
            .Select(a => new UpcomingActivities
            {
                ActivityId = a.Id,
                ActivityName = a.Name,
                QualificationDate = a.QualificationDate,

                CourseId = a.Unit.Course.Id,
                CourseName = a.Unit.Course.Name,

                CenterId = a.Unit.Course.Center.Id,
                CenterName = a.Unit.Course.Center.Name
            })
            .Take(5)
            .ToListAsync();

        return result;
    }

    public async Task<List<PendingActivities>> GetTopCoursesWithMostPendingActivitiesAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
        var currentDate = DateTime.UtcNow;

        var result = await context.Activities
            .AsNoTracking()
            .Where(a => a.QualificationDate < currentDate) // actividades vencidas
            .Where(a => a.Unit.Course.IsActive) // clases activas
            .Where(a => !a.Unit.Course.Center.IsArchived && a.Unit.Course.Center.TeacherId == userId) // centro válido y del profe
            .Select(a => new
            {
                Activity = a,
                Course = a.Unit.Course,
                CourseId = a.Unit.Course.Id,
                CourseName = a.Unit.Course.Name,
                CourseCode = a.Unit.Course.Code,
                // una consulta interna
                TotalStudentsInCourse = context.StudentsCourses
                    .Count(sc => sc.CourseId == a.Unit.Course.Id && sc.IsActive),

                StudentsWithNote = a.StudentNotes.Count()
            })
            // si los alumnos con notas no cuadra con los alumnos activos en la clase
            .Where(x => x.StudentsWithNote < x.TotalStudentsInCourse) // actividad pendiente
            .GroupBy(x => new { x.CourseId, x.CourseName, x.CourseCode })
            .Select(g => new PendingActivities
            {
                CourseId = g.Key.CourseId,
                CourseName = g.Key.CourseName,
                CourseCode = g.Key.CourseCode,
                PendingActivitiesCount = g.Count()
            })
            .OrderByDescending(pa => pa.PendingActivitiesCount)
            .Take(3)
            .ToListAsync();

        return result;
    }

    public async Task<GeneralStadistics> GeneralStadisticsAsync ( string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

        var query = from studentCourse in context.StudentsCourses.AsNoTracking()
                    where studentCourse.IsActive
                    let course = studentCourse.Course
                    where course.IsActive
                    let center = course.Center
                    where center.TeacherId == userId && !center.IsArchived
                    select new
                    {
                        CenterId = center.Id,         
                        CourseId = course.Id,         
                        StudentId = studentCourse.StudentId
                    };

        var result = await query
            .GroupBy(x => 1) // Agrupar todo en uno solo para contar distintos elementos
            .Select(g => new GeneralStadistics
            {
                TotalCentersCount = g.Select(x => x.CenterId).Distinct().Count(),
                TotalClassesCount = g.Select(x => x.CourseId).Distinct().Count(),
                TotalStudentsCount = g.Select(x => x.StudentId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        // Por si no hay datos (evita null)
        return result ?? new GeneralStadistics();

    }

}
