using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboarCenter;
using ClassNotes.API.Services.Audit;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;


namespace ClassNotes.API.Services.DashboarCenter
{
    public class DashboardCenterService : IDashboardCenterService
    {
        private readonly ClassNotesContext _context;
        private readonly ILogger<DashboardCenterService> _logger;
        private readonly IAuditService _auditService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int PAGE_SIZE;

        public DashboardCenterService(
                ClassNotesContext context,
                ILogger<DashboardCenterService> logger,
                IAuditService auditService,
                IConfiguration configuration,
                IServiceScopeFactory scopeFactory
            )
        {
            this._context = context;
            this._logger = logger;
            this._auditService = auditService;
            this._scopeFactory = scopeFactory;
            //JA: Accedemos al tamanio de la pagina de centros
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Centers");
        }

        public async Task<ResponseDto<DashboardCenterDto>> GetDashboardCenterAsync(
        Guid centerId,
        string searchTerm = "",
        int page = 1,
        int? pageSize = null,
        string classType = "ACTIVE")
        {
             int MAX_PAGE_SIZE = 16;
        // Validación y configuración de paginación
            int currentPageSize = Math.Min(pageSize ?? PAGE_SIZE, MAX_PAGE_SIZE);
            currentPageSize = Math.Max(1, currentPageSize);
            int startIndex = (page - 1) * currentPageSize;

            // Validar classType
            classType = (classType ?? "ACTIVE").ToUpper();
            if (!new[] { "ACTIVE", "INACTIVE", "ALL" }.Contains(classType))
            {
                classType = "ACTIVE";
            }

            // Obtener el centro con sus relaciones
            var center = await _context.Centers
                .Where(c => c.TeacherId == _auditService.GetUserId() &&
                            c.Id == centerId &&
                            c.IsArchived == false)
                .Include(c => c.Courses)
                    .ThenInclude(c => c.Students)
                .Include(c => c.Courses)
                    .ThenInclude(c => c.Units)
                        .ThenInclude(u => u.Activities)
                .FirstOrDefaultAsync();

            if (center == null)
            {
                return new ResponseDto<DashboardCenterDto>
                {
                    StatusCode = 404,
                    Message = MessagesConstant.RECORD_NOT_FOUND,
                    Status = false,
                };
            }

            // Procesamiento paralelo seguro
            var (totalStudents, totalCourses, pendingActivities) = await ProcessCenterDataInParallel(center, classType);

            // Consulta y paginación de cursos
            var (activeClasses, totalItems, globalAttendance) = await ProcessCoursesData(
                center.Id,
                searchTerm,
                classType,
                startIndex,
                currentPageSize);

            return new ResponseDto<DashboardCenterDto>
            {
                StatusCode = 200,
                Data = new DashboardCenterDto
                {
                    Summary = new DashboarCenterSummaryDto
                    {
                        TotalStudents = totalStudents,
                        TotalCourses = totalCourses,
                        PendingActivities = pendingActivities,
                        AverageAttendance = globalAttendance
                    },
                    ActiveClasses = new PaginationDto<List<DashboarCenterActiveClassDto>>
                    {
                        CurrentPage = page,
                        PageSize = currentPageSize,
                        TotalItems = totalItems,
                        TotalPages = (int)Math.Ceiling(totalItems / (double)currentPageSize),
                        HasPreviousPage = page > 1,
                        HasNextPage = page * currentPageSize < totalItems,
                        Items = activeClasses
                    },
                },
                Message = MessagesConstant.RECORD_FOUND,
                Status = true,
            };
        }

        private async Task<(int totalStudents, int totalCourses, int pendingActivities)>
            ProcessCenterDataInParallel(CenterEntity center, string classType)
        {
            // Procesar datos que no requieren DbContext adicional
            var totalStudents = center.Courses?
                .SelectMany(c => c.Students)?
                .Count() ?? 0;

            var totalCourses = classType switch
            {
                "ACTIVE" => center.Courses.Count(c => c.IsActive),
                "INACTIVE" => center.Courses.Count(c => !c.IsActive),
                _ => center.Courses.Count
            };

            var pendingActivities = await CalculatePendingActivities(center, classType);

            return (totalStudents, totalCourses, pendingActivities);
        }

        private async Task<int> CalculatePendingActivities(CenterEntity center, string classType)
        {
            var filteredCourses = center.Courses
                .Where(c => classType == "ALL" || c.IsActive == (classType == "ACTIVE"))
                .ToList();

            var activityIds = filteredCourses
                .SelectMany(c => c.Units)
                .SelectMany(u => u.Activities)
                .Where(a => a.QualificationDate < DateTime.UtcNow)
                .Select(a => a.Id)
                .ToList();

            if (!activityIds.Any())
                return 0;

            var activeStudentIdsPerCourse = filteredCourses
                .SelectMany(c => c.Students)
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            var completedActivities = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

            await Parallel.ForEachAsync(activityIds, parallelOptions, async (activityId, ct) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var localContext = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();

                try
                {
                    var gradedStudentsCount = await localContext.StudentsActivitiesNotes
                        .Where(san => san.ActivityId == activityId)
                        .Select(san => san.StudentId)
                        .Distinct()
                        .CountAsync(ct);

                    if (gradedStudentsCount == activeStudentIdsPerCourse.Count)
                    {
                        Interlocked.Increment(ref completedActivities);
                    }
                }
                finally
                {
                    if (localContext is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                }
            });

            return Math.Max(activityIds.Count - completedActivities, 0);
        }

        private async Task<(List<DashboarCenterActiveClassDto> classes, int totalItems, double globalAttendance)>
            ProcessCoursesData(Guid centerId, string searchTerm, string classType, int startIndex, int pageSize)
        {
            var query = _context.Courses
                .Include(c => c.Attendances)
                .Include(c => c.Students)
                .Where(c => c.CenterId == centerId);

            if (classType != "ALL")
            {
                bool isActive = classType == "ACTIVE";
                query = query.Where(c => c.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm) || c.Code.Contains(searchTerm));
            }

            var totalItems = await query.CountAsync();

            var activeClasses = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip(startIndex)
                .Take(pageSize)
                .Select(c => new DashboarCenterActiveClassDto
                {

                    IdCourse = c.Id,
                    isActive = c.IsActive,
                    CourseName = c.Name,
                    CourseCode = c.Code,
                    StudentCount = c.Students.Count,
                    AverageAttendance = c.Attendances.Any()
                        ? Math.Round((double)c.Attendances.Count(a => a.Attended) / c.Attendances.Count * 100, 2)
                        : 0,
                    ActivityStatus = new DashboardCenterActivityStatusDto
                    {
                        Total = c.Units.SelectMany(u => u.Activities).Count(),
                        CompletedCount = c.Units.SelectMany(u => u.Activities)
                            .Count(a => _context.StudentsActivitiesNotes
                                .Where(san => san.ActivityId == a.Id)
                                .Select(san => san.StudentId)
                                .Distinct()
                                .Count() == c.Students.Count),
                        PendingCount = c.Units.SelectMany(u => u.Activities)
                            .Count(a => a.QualificationDate < DateTime.UtcNow &&
                                _context.StudentsActivitiesNotes
                                    .Where(san => san.ActivityId == a.Id)
                                    .Select(san => san.StudentId)
                                    .Distinct()
                                    .Count() < c.Students.Count),
                        NextActivity = c.Units
                            .SelectMany(u => u.Activities)
                            .Where(a => a.QualificationDate > DateTime.Now)
                            .OrderBy(a => a.QualificationDate)
                            .Select(a => a.Name)
                            .FirstOrDefault() ?? "Ninguna",
                        NextActivityDate = c.Units
                            .SelectMany(u => u.Activities)
                            .Where(a => a.QualificationDate > DateTime.Now)
                            .OrderBy(a => a.QualificationDate)
                            .Select(a => a.QualificationDate)
                            .FirstOrDefault(),
                        LastExpiredDate = c.Units
                            .SelectMany(u => u.Activities)
                            .Where(a => a.QualificationDate <= DateTime.Now)
                            .OrderByDescending(a => a.QualificationDate)
                            .Select(a => a.QualificationDate)
                            .FirstOrDefault()
                    }
                })
                .ToListAsync();

            var globalAttendance = await CalculateGlobalAttendance(query);

            return (activeClasses, totalItems, globalAttendance);
        }

        private async Task<double> CalculateGlobalAttendance(IQueryable<CourseEntity> query)
        {
            var attendances = await query
                .Select(c => new
                {
                    HasAttendance = c.Attendances.Any(),
                    Avg = c.Attendances.Any()
                        ? Math.Round((double)c.Attendances.Count(a => a.Attended) / c.Attendances.Count * 100, 2)
                        : 0
                })
                .ToListAsync();

            return attendances
                .Where(x => x.HasAttendance)
                .Select(x => x.Avg)
                .DefaultIfEmpty(0)
                .Average();
        }
    }
}
