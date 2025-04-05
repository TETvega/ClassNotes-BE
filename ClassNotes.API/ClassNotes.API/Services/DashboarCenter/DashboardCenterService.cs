using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboarCenter;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;


namespace ClassNotes.API.Services.DashboarCenter
{
    public class DashboardCenterService : IDashboardCenterService
    {
        private readonly ClassNotesContext _context;
        private readonly ILogger<DashboardCenterService> _logger;
        private readonly IAuditService _auditService;
        private readonly int PAGE_SIZE;

        public DashboardCenterService(
                ClassNotesContext context,
                ILogger<DashboardCenterService> logger,
                IAuditService auditService,
                IConfiguration configuration
            )
        {
            this._context = context;
            this._logger = logger;
            this._auditService = auditService;
            //JA: Accedemos al tamanio de la pagina de centros
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Centers");
        }

        public async Task<ResponseDto<DashboardCenterDto>> GetDashboardCenterAsync(Guid centerId, string searchTerm = "", int page = 1)
        {
            //JA: Calculamos el Indice de inicio para la paginaciOn
            int startIndex = (page - 1) * PAGE_SIZE;

            //JA: Obtiene el ID del usuario autenticado
            var userId = _auditService.GetUserId();

            //JA: Busca el centro basado en el usuario autenticado y el ID del centro proporcionado
            var center = await _context.Centers
                .Where(c => c.TeacherId == userId && c.Id == centerId && c.IsArchived == false)//JA: Validamos que el centro no este archivado
                .Include(c => c.Courses) // Cargar cursos
                .ThenInclude(c => c.Students) // Cargar estudiantes de cada curso
                .Include(c => c.Courses)
                .ThenInclude(c => c.Units) // Cargar unidades de cada curso
                .ThenInclude(u => u.Activities) // Cargar actividades de las unidades
                .FirstOrDefaultAsync();

            //JA: Si hay error devolvemos el mensaje
            if (center == null)
            {
                return new ResponseDto<DashboardCenterDto>
                {
                    StatusCode = 404,
                    Message = MessagesConstant.RECORD_NOT_FOUND,
                    Status = false,
                };
            }
            //JA: Obtener total de estudiantes en el centro
            var totalStudents = center.Courses?.SelectMany(c => c.Students)?.Count() ?? 0;

            //JA: Total de cursos de ese centro que estan activos
            var totalCourses = center.Courses?.Count(c => c.IsActive) ?? 0;

     // JA: Calcular las actividades pendientes de ese centro

            // 1. Obtener todas las actividades vencidas de cursos activos
            var activityIds = center.Courses
                .Where(c => c.IsActive) // Filtra solo los cursos activos
                .SelectMany(c => c.Units)
                .SelectMany(u => u.Activities)
                .Where(a => a.QualificationDate < DateTime.UtcNow) // Solo actividades vencidas
                .Select(a => a.Id)
                .ToList();

            // 2. Obtener todos los estudiantes activos del centro
            var activeStudentIdsPerCourse = center.Courses
                .Where(c => c.IsActive) // Filtra solo los cursos activos
                .SelectMany(c => c.Students)
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            var activitiesCount = activityIds.Count;
            var completedActivitiesCount = 0;

            // 3. Validar cuántas actividades han sido completadas
            foreach (var activityId in activityIds)
            {
                var gradedStudents = await _context.StudentsActivitiesNotes
    .Where(san => san.ActivityId == activityId)
    .Select(san => san.StudentId)
    .Distinct()
    .ToListAsync();

                if (gradedStudents.Count == activeStudentIdsPerCourse.Count)
                {
                    completedActivitiesCount++;
                }
            }

            // 4. Calcular actividades pendientes
            var pendingActivitiesCount = activityIds.Count - completedActivitiesCount;
            pendingActivitiesCount = Math.Max(pendingActivitiesCount, 0); // Evita valores negativos




            // JA: Obtenemos las clases del centro que estan activas
            var query = _context.Courses
                .Include(c => c.Attendances)
                .Include(c => c.Students)
                .Where(c => c.CenterId == center.Id && c.IsActive == true);//JA: Validamos que nuestro curso este activo

            //JA: Filtramos y buscamos por el parametro que recibimos
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm) || c.Code.Contains(searchTerm));
            }

            //JA: Obtener total de elementos antes de paginar
            var totalItems = await query.CountAsync();

            //JA: Aplicamos la paginacion a los items por orden de fecha de creacion
            var activeClasses = await query
           .OrderByDescending(c => c.CreatedDate)
           .Skip(startIndex)
           .Take(PAGE_SIZE)
           .Select(c => new DashboarCenterActiveClassDto
           {
               IdCourse = c.Id,
               CourseName = c.Name,
               CourseCode = c.Code,
               //JA: Aqui sacamos cuantos estudiantes tiene cada curso
               StudentCount = c.Students.Count(),
               //JA: Aqui obtenemos la asistencia Promedio de la clase, si no hay asistencia devolvemos 0
               AverageAttendance = c.Attendances.Count() > 0
    ? Math.Round((double)c.Attendances.Count(a => a.Attended) / c.Attendances.Count() * 100, 2)
    : 0,


               //TODO JA: Calculamos la nota promedio del curso

               //JA: Aqui devolvemos las actividad mas proxima a vencer
               ActivityStatus = new DashboardCenterActivityStatusDto
               {
                   //JA: Contamos las actividades de todas las unidades
                   Total = c.Units.SelectMany(u => u.Activities).Count(),
                   //JA: Contamos las actividades completadas
                   CompletedCount = c.Units.SelectMany(u => u.Activities)
    .Count(a => _context.StudentsActivitiesNotes
        .Where(san => san.ActivityId == a.Id)
        .Select(san => san.StudentId)
        .Distinct()
        .Count() == c.Students.Count() // Si la cantidad de notas registradas es igual a los estudiantes del curso
    ),



                   PendingCount = c.Units.SelectMany(u => u.Activities)
    .Count(a => a.QualificationDate < DateTime.UtcNow // Fecha de vencimiento expirada
        && _context.StudentsActivitiesNotes
            .Where(san => san.ActivityId == a.Id)
            .Select(san => san.StudentId)
            .Distinct()
            .Count() < c.Students.Count() // Sigue pendiente si no todos han sido evaluados
    ),



                   //JA: Mostramos la siguiente actividad a vencer
                   NextActivity = c.Units
                   .SelectMany(u => u.Activities)
                   .Where(a => a.QualificationDate > DateTime.Now)
                   .OrderBy(a => a.QualificationDate)
                   .Select(a => a.Name)
                   .FirstOrDefault() ?? "Ninguna", // Asignamos "Ninguna" si no hay actividades
                                                   //JA: Mandamos la fecha de la siguiente actividad
                   NextActivityDate = c.Units
                   .SelectMany(u => u.Activities)
                   .Where(a => a.QualificationDate > DateTime.Now)
                   .OrderBy(a => a.QualificationDate)
                   .Select(a => a.QualificationDate)
                   .FirstOrDefault(),
                   //JA: Mandamos la fecha de la ultima actividad vencida por si acaso
                   LastExpiredDate = c.Units
                   .SelectMany(u => u.Activities)
                   .Where(a => a.QualificationDate <= DateTime.Now)
                   .OrderByDescending(a => a.QualificationDate)
                   .Select(a => a.QualificationDate)
                   .FirstOrDefault()
               }

           }).ToListAsync();

            //JA: Creamos un objeto de paginacio ya que si no lo hacemos cuando enviemos los items nos saldra error por que la paginacion espera un objeto no una lista,
            var paginatedClasses = new PaginationDto<List<DashboarCenterActiveClassDto>>
            {
                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)PAGE_SIZE),
                HasPreviousPage = page > 1,
                HasNextPage = page * PAGE_SIZE < totalItems,
                Items = activeClasses
            };

            //JA: Calculamos la asistencia promedio global de todos los cursos del centro, excluyendo aquellos con asistencia promedio de 0
            var globalAverageAttendance = activeClasses
            .Where(c => c.AverageAttendance > 0) //JA: Filtramos los cursos con asistencia mayor a 0
            .Select(c => c.AverageAttendance)    //JA: Solo seleccionamos los valores numéricos
            .DefaultIfEmpty(0)                   //JA: Si la secuencia esta vacía, usamos 0 para que no devuelva error
            .Average();                           //JA: Calculamos el promedio

            //JA: Si hay cursos con asistencia promedio de 0, la omitimos a la hora de sacar el promedio global
            globalAverageAttendance = activeClasses.Any(c => c.AverageAttendance > 0)
            ? Math.Round(globalAverageAttendance, 2)
            : 0;

            //JA: Retornamos el objeto DTO con el resumen general y las clases activas
            return new ResponseDto<DashboardCenterDto>
            {
                StatusCode = 200,
                Data = new DashboardCenterDto
                {
                    Summary = new DashboarCenterSummaryDto
                    {
                        TotalStudents = totalStudents,
                        TotalCourses = totalCourses,
                        PendingActivities = pendingActivitiesCount, //JA: Enviamos la cantidad de actividades pendientes por evaluar
                        AverageAttendance = globalAverageAttendance
                    },
                    //JA: Enviamos las clases paginadas
                    ActiveClasses = paginatedClasses,
                },
                Message = MessagesConstant.RECORD_FOUND,
                Status = true,
            };
        }
    }
}
