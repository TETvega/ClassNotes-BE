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

        public DashboardCenterService(
                ClassNotesContext context,
                ILogger<DashboardCenterService> logger,
                IAuditService auditService
            )
        {
            this._context = context;
            this._logger = logger;
            this._auditService = auditService;
        }

        public async Task<ResponseDto<DashboardCenterDto>> GetDashboardCenterAsync(Guid centerId)
        {
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
                    Status=false,
                };
            }
            //JA: Obtener total de estudiantes en el centro
            var totalStudents = center.Courses?.SelectMany(c => c.Students)?.Count() ?? 0;

            //JA: Total de cursos de ese centro que estan activos
            var totalCourses = center.Courses?.Count(c => !c.IsActive) ?? 0;

            //JA: Calcular las actividades pendientes de ese centro
            var pendingActivities = center.Courses?
                .SelectMany(c => c.Units)
                .SelectMany(u => u.Activities)
                .Count(a => a.QualificationDate > DateTime.Now) ?? 0;

            //JA: Obtener las clases que estan activas
            var activeClasses = _context.Courses
            .Include(c => c.Attendances) //JA: incluimos las asistencias
            .Include(c => c.Students) //JA: Incluimos los estudiantes también
            .Where(c => c.CenterId == center.Id && c.IsActive == false)
            .Select(c => new DashboarCenterActiveClassDto
            {
                IdCourse = c.Id,
                CourseName = c.Name,
                CourseCode = c.Code,
                //JA: Aqui sacamos cuantos estudiantes tiene cada curso
                StudentCount = c.Students.Count(),
                //JA: Aqui obtenemos la asistencia Promedio de la clase, si no hay asistencia devolvemos 0
                AverageAttendance = c.Attendances.Count() > 0
                ? (double)c.Attendances.Count(a => a.Attended) / c.Attendances.Count() * 100 : 0,
                //JA: Aqui la nota promedio del curso

                //TODO JA: Calculamos la nota promedio del curso

                //JA: Aqui devolvemos las actividad mas proxima a vencer
                ActivityStatus = new DashboardCenterActivityStatusDto
                {
                    //JA: Contamos las actividades de todas las unidades
                    Total = c.Units.SelectMany(u => u.Activities).Count(),
                    //JA: Contamos las actividades completadas
                    CompletedCount = c.Units
                            .SelectMany(u => u.Activities)
                            .Count(a => a.QualificationDate <= DateTime.Now),
                    //JA: contamos las actividades pendientes
                    PendingCount = c.Units
                            .SelectMany(u => u.Activities)
                            .Count(a => a.QualificationDate > DateTime.Now),
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
            }).ToList();

            //JA: Calculamos la asistencia promedio global de todos los cursos del centro, excluyendo aquellos con asistencia promedio de 0
            var globalAverageAttendance = activeClasses
            .Where(c => c.AverageAttendance > 0) //JA: Filtramos los cursos con asistencia mayor a 0
            .Select(c => c.AverageAttendance)    //JA: Solo seleccionamos los valores numéricos
            .DefaultIfEmpty(0)                   //JA: Si la secuencia esta vacía, usamos 0 para que no devuelva error
            .Average();                           //JA: Calculamos el promedio

            //JA: Si hay cursos con asistencia promedio de 0, la omitimos a la hora de sacar el promedio global
            globalAverageAttendance = activeClasses.Any(c => c.AverageAttendance > 0)
                ? globalAverageAttendance
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
                        PendingActivities = pendingActivities,
                        AverageAttendance = globalAverageAttendance
                    },
                    ActiveClasses = activeClasses
                },
                Message = MessagesConstant.RECORD_FOUND,
                Status = true,
            }; 
        }
    }
}
