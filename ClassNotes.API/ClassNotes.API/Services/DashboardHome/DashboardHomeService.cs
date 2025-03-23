using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Dashboard;
using ClassNotes.API.Dtos.DashboardHome;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;

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
        var userId = _auditService.GetUserId();

        //CG: Retornar el total de centros que tenga el usuario que esten activos (no esten archivados)
        var centersCount = await _context.Centers
            .Where(c => c.TeacherId == userId && !c.IsArchived)
            .CountAsync();

        //CG: Retornar el total de cursos activos que tenga el usuario
        var coursesCount = await _context.Courses
            .Where(c => c.Center.TeacherId == userId && c.IsActive)
            .CountAsync();

        //CG: Obtener los Id de los cursos activos del docente
        var courseIds = await _context.Courses
            .Where(c => c.Center.TeacherId == userId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        // Obtener los estudiantes activos en los cursos activos del docente
        var activeStudentIds = await _context.StudentsCourses
            .Where(sc => courseIds.Contains(sc.CourseId)) // Filtra por cursos activos del docente
            .Where(sc => sc.IsActive) // Solo estudiantes activos en el curso
            .Select(sc => sc.StudentId)
            .Distinct() // Evitar duplicados si un estudiante está en varios cursos
            .ToListAsync();

        //CG: Retornar el total de estudiantes de las clases que esten activas
        var studentsCount = activeStudentIds.Count();

        // --------------------------- Creacion del List<DashboardHomePendingActivityDto> --------------------------

        var courseIdsForGraphics = courseIds.Take(3).ToList();          //Para la graficas solo se ocupan 3 cursos

        var pendingActivitiesDto = new List<DashboardHomePendingActivityDto>();     //variable que se usará dentro del foreach de courseIds y como respuesta

        //CG: Diccionario para almacenar el conteo de estudiantes activos por curso
        var activeStudentsPerCourse = new Dictionary<Guid, int>();

        //CG: Diccionario para almacenar los StudentId activos por curso
        var activeStudentsIdsPerCourse = new Dictionary<Guid, List<Guid>>();

        //CG: Diccionario para almacenar el conteo de actividades por curso (IsExtra == false)
        var activitiesCountPerCourse = new Dictionary<Guid, int>();

        //CG: Diccionario para almacenar los ActivityId por curso (IsExtra == false)
        var activityIdsPerCourse = new Dictionary<Guid, List<Guid>>();

        //CG: Diccionario para almacenar el total de actividades * estudiantes por curso
        var activitiesStudentsTotalPerCourse = new Dictionary<Guid, int>();

        //CG: Diccionario para almacenar el conteo de registros en StudentsActivitiesNotes por curso
        var activitiesNotesCountPerCourse = new Dictionary<Guid, int>();

        //CG: Recorrer cada curso y contar los estudiantes activos y actividades que no sean extras
        foreach (var courseId in courseIds)
        {
            //CG: Obtener los StudentId activos en el curso
            var activeStudentIdsPerCourse = await _context.StudentsCourses
                .Where(sc => sc.CourseId == courseId && sc.IsActive)
                .Select(sc => sc.StudentId)
                .ToListAsync();

            //CG: Almacenar el conteo en el diccionario
            activeStudentsPerCourse[courseId] = activeStudentIdsPerCourse.Count;

            //CG: Almacenar los StudentId en el diccionario
            activeStudentsIdsPerCourse[courseId] = activeStudentIdsPerCourse;

            //CG: Obtener los id's de actividades del curso (IsExtra == false)
            var activityIds = await _context.Units
                .Where(u => u.CourseId == courseId)     // Filtra por unidades del curso
                .SelectMany(u => u.Activities)          // Obtiene las actividades de las unidades
                .Where(a => !a.IsExtra)                 // Filtra por actividades no extra
                .Select(a => a.Id)                      // Selecciona los Id de las actividades
                .ToListAsync();

            //CG: Almacenar el conteo de actividades en el diccionario
            activitiesCountPerCourse[courseId] = activityIds.Count;

            //CG: Almacenar los ActivityId en el diccionario
            activityIdsPerCourse[courseId] = activityIds;

            //CG: Calcular el total de actividades * estudiantes para el curso
            var totalActivitiesStudents = activeStudentsPerCourse[courseId] * activitiesCountPerCourse[courseId];

            //CG: Almacenar el total en el diccionario
            activitiesStudentsTotalPerCourse[courseId] = totalActivitiesStudents;

            //CG: Contar los registros en StudentsActivitiesNotes para el curso
            var activitiesNotesCount = 0;
            foreach (var activityId in activityIds)
            {
                foreach (var studentId in activeStudentIdsPerCourse)
                {
                    var count = await _context.StudentsActivitiesNotes
                        .Where(san => san.ActivityId == activityId && san.StudentId == studentId)
                        .CountAsync();
                    activitiesNotesCount += count;
                }
            }

            //CG: Almacenar el conteo en el diccionario
            activitiesNotesCountPerCourse[courseId] = activitiesNotesCount;

            //CG: Calcular la diferencia entre el total y el conteo de registros
            var difference = activitiesStudentsTotalPerCourse[courseId] - activitiesNotesCountPerCourse[courseId];

            //CG: Obtener el nombre del curso
            var courseInfo = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new
                {
                    c.Name,
                    c.Code,
                })
                .FirstOrDefaultAsync();

            //CG: Crear una instancia de DashboardHomePendingActivityDto
            var pendingActivityDto = new DashboardHomePendingActivityDto
            {
                Id = courseId,
                Number = difference,
                CourseName = courseInfo.Name,
                CourseCode = courseInfo.Code,
            };

            //CG: Agregar a la lista de actividades pendientes
            pendingActivitiesDto.Add(pendingActivityDto);

            // Imprimir en la terminal (log)
            //_logger.LogInformation($"Curso ID: {courseId}, Estudiantes Activos: {activeStudentIds.Count}");
            //_logger.LogInformation($"Curso ID: {courseId}, IDs de Estudiantes Activos: {string.Join(", ", activeStudentIds)}");
            //_logger.LogInformation($"Curso ID: {courseId}, Actividades: {activityIds.Count}");
            //_logger.LogInformation($"Curso ID: {courseId}, IDs de Actividades: {string.Join(", ", activityIds)}");
            //_logger.LogInformation($"Curso ID: {courseId}, Total: {totalActivitiesStudents}");
            //_logger.LogInformation($"Curso ID: {courseId}, Registros en StudentsActivitiesNotes: {activitiesNotesCount}");
            //_logger.LogInformation($"Curso ID: {courseId}, Diferencia: {difference}");
        }

        // --------------------------- Creacion del List<DashboardHomePendingActivityDto> --------------------------

        // --------------------------- Creacion del List<DashboardHomeUpcomingActivityDto> --------------------------
        var upcomingActivitiesDto = new List<DashboardHomeUpcomingActivityDto>();

        //CG: Obtener las próximas actividades (las 4 más cercanas a expirar)
        var upcomingActivities = await _context.Activities
            .Where(a => a.QualificationDate > DateTime.Now && a.Unit.Course.Center.TeacherId == userId) // Filtra por actividades con fecha futura
            .OrderBy(a => a.QualificationDate) // Ordena por fecha de calificación (de menor a mayor)
            .Take(4) // Toma las primeras 4 actividades
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.QualificationDate,
                CourseName = a.Unit.Course.Name // Obtener el nombre del curso a través de la unidad
            })
            .ToListAsync();

        foreach (var activity in upcomingActivities)
        {
            var upcomingActivityDto = new DashboardHomeUpcomingActivityDto
            {
                Id = activity.Id,
                Name = activity.Name,
                CourseName = activity.CourseName,
                QualificationDate = activity.QualificationDate
            };

            // Agregar a la lista de próximas actividades
            upcomingActivitiesDto.Add(upcomingActivityDto);

            // Imprimir en la terminal (log)
        }

        // --------------------------- Creacion del List<DashboardHomeUpcomingActivityDto> --------------------------

        // --------------------------- Creacion del List<DashboardHomeCenterDto> --------------------------
        var centersDto = new List<DashboardHomeCenterDto>();

        //CG: Obtener los primeros 3 centros activos del docente
        var centerIds = await _context.Centers
            .Where(c => c.TeacherId == userId && !c.IsArchived) // Filtra por centros no archivados del docente
            .Select(c => c.Id)
            .Take(3) // Tomar solo 3 según diseño del dashboard
            .ToListAsync();

        foreach (var centerId in centerIds)
        {
            //CG: Obtener los detalles del centro
            var centerDetails = await _context.Centers
                .Where(c => c.Id == centerId)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Abbreviation,
                    c.Logo
                })
                .FirstOrDefaultAsync();

            //CG: Obtener los cursos activos del centro
            var activeCourseIdsByCenter = await _context.Courses
                .Where(c => c.CenterId == centerId && c.IsActive) // Filtra por cursos activos del centro
                .Select(c => c.Id)
                .ToListAsync();

            //CG: Calcular el CoursesCount
            var coursesCountByCenter = activeCourseIdsByCenter.Count;

            //CG: Calcular el StudentsCount
            var studentsCountByCenter = await _context.StudentsCourses
                .Where(sc => activeCourseIdsByCenter.Contains(sc.CourseId) && sc.IsActive) // Filtra por estudiantes activos en los cursos del centro
                .Select(sc => sc.StudentId)
                //.Distinct() // Evitar duplicados si un estudiante está en varios cursos
                .CountAsync();

            // Crear una instancia de DashboardHomeCenterDto
            var centerDto = new DashboardHomeCenterDto
            {
                Id = centerDetails.Id,
                Name = centerDetails.Name,
                Abbreviation = centerDetails.Abbreviation,
                Logo = centerDetails.Logo,
                CoursesCount = coursesCountByCenter,
                StudentsCount = studentsCountByCenter
            };

            // Agregar a la lista de centros
            centersDto.Add(centerDto);

            // Imprimir en la terminal (log)

        }

        // --------------------------- Creacion del List<DashboardHomeCenterDto> --------------------------

        // --------------------------- Creacion del List<DashboardHomeActiveCourseDto> --------------------------

        var activeCoursesDto = new List<DashboardHomeActiveCourseDto>();     //variable que se usará dentro del foreach de courseIds y como respuesta

        //CG: Obtener los primeros 4 cursos activos
        var activeCourseIds = await _context.Courses
            .Where(c => c.Center.TeacherId == userId && c.IsActive)
            .Select(c => c.Id)
            .Take(4) // Tomar solo 4 según diseño del dashboard
            .ToListAsync();

        foreach (var courseId in activeCourseIds)
        {
            //CG: Obtener los StudentId activos en el curso
            var activeStudentIdsPerCourse = await _context.StudentsCourses
                .Where(sc => sc.CourseId == courseId && sc.IsActive)
                .Select(sc => sc.StudentId)
                .ToListAsync();

            //CG: Obtener los id's de actividades del curso (IsExtra == false)
            var activityIds = await _context.Units
                .Where(u => u.CourseId == courseId) // Filtra por unidades del curso
                .SelectMany(u => u.Activities) // Obtiene las actividades de las unidades
                .Where(a => !a.IsExtra) // Filtra por actividades no extra
                .Select(a => a.Id) // Selecciona los Id de las actividades
                .ToListAsync();

            //CG: Calcular el total de actividades y iniciar contador para saber cuantas actividades ya fueron revisadas a todos los estudiantes
            var activitiesCount = activityIds.Count;
            var completedActivitiesCount = 0;
            foreach (var activityId in activityIds)
            {
                //CG: Contar los registros en StudentsActivitiesNotes para la actividad
                var activitiesNotesCount = 0;
                
                foreach (var studentId in activeStudentIdsPerCourse)
                {
                    var count = await _context.StudentsActivitiesNotes
                        .Where(san => san.ActivityId == activityId && san.StudentId == studentId)
                        .CountAsync();
                    activitiesNotesCount += count;
                }

                //CG: Debe existir igualdad entre el numero de registros y el numero de estudiantes activos
                if (activitiesNotesCount == activeStudentIdsPerCourse.Count)
                {
                    completedActivitiesCount++;
                }
            }

            //CG: Obtener el nombre, código y abreviación del centro del curso
            var courseDetails = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new
                {
                    c.Name,
                    c.Code,
                    CenterAbbreviation = c.Center.Abbreviation
                })
                .FirstOrDefaultAsync();

            //CG: Crear una instancia de DashboardHomeActiveCourseDto
            var activeCourseDto = new DashboardHomeActiveCourseDto
            {
                Id = courseId,
                Name = courseDetails.Name,
                Code = courseDetails.Code,
                CenterAbbreviation = courseDetails.CenterAbbreviation,
                StudentsCount = activeStudentIdsPerCourse.Count,
                CompletedActivitiesCount = completedActivitiesCount,
                ActivitiesCount = activitiesCount
            };

            //CG: Agregar a la lista de cursos activos
            activeCoursesDto.Add(activeCourseDto);

            //Aqui iria la parte de los logs de logger
        }

        // --------------------------- Creacion del List<DashboardHomeActiveCourseDto> --------------------------

        // --------------------------- Creacion del List<DashboardHomeStudentDto> --------------------------

        var studentsDto = new List<DashboardHomeStudentDto>();

        //CG: Se hará uso de courseIds de la linea 44 para saber los id's de los estudiantes del usuario
        //CG: Se hará uso de activeStudentIds de la linea 50 para saber los id's de los estudiantes activos del usuario

        //CG: Diccionario para almacenar el número de actividades pendientes por estudiante
        var pendingCounts = new Dictionary<Guid, int>();

        foreach (var studentId in activeStudentIds) {
            //CG: Obtener los cursos activos del estudiante, usando el id estudiante que se este recorriendo y el id del curso exista y este activo
            var activeCourseIdsForStudent = await _context.StudentsCourses
                .Where(sc => sc.StudentId == studentId && courseIds.Contains(sc.CourseId) && sc.IsActive)
                .Select(sc => sc.CourseId)
                .ToListAsync();

            //CG: Calcular el número de actividades pendientes para el estudiante
            var pendingsCountForStudent = 0;

            foreach (var courseId in activeCourseIdsForStudent)
            {
                //CG: Obtener las actividades del curso (IsExtra == false)
                var activityIds = await _context.Units
                    .Where(u => u.CourseId == courseId)
                    .SelectMany(u => u.Activities)
                    .Where(a => !a.IsExtra)
                    .Select(a => a.Id)
                    .ToListAsync();

                //CG: Contar los registros en StudentsActivitiesNotes para el estudiante
                var activitiesNotesCount = 0;

                foreach (var activityId in activityIds)
                {
                    var count = await _context.StudentsActivitiesNotes
                        .Where(san => san.ActivityId == activityId && san.StudentId == studentId)
                        .CountAsync();

                    activitiesNotesCount += count;
                }

                //CG: Calcular la diferencia entre el total de actividades y las actividades calificadas
                pendingsCountForStudent += activityIds.Count - activitiesNotesCount;
            }

            //CG: Almacenar el número de actividades pendientes en el diccionario
            pendingCounts[studentId] = pendingsCountForStudent;
        }

        //CG: Ordenar los estudiantes por el número de actividades pendientes (en orden descendente)
        var sortedStudentIds = pendingCounts
            .OrderByDescending(pair => pair.Value)      //Los valores mayores iran de primero
            .ThenBy(pair => pair.Key)                   //En caso de que hayan mas de dos valores con el mismo Value, "desempatar" segun el orden de su Id
            .Select(pair => pair.Key)                   //Tomar solo el id de los estudiantes para poder obtener mas informacion de ellos
            .Take(8) // Tomar solo los 8 estudiantes con más pendientes
            .ToList();

        foreach (var studentId in sortedStudentIds)
        {
            var studentDetails = await _context.Students
                .Where(s => s.Id == studentId)
                .Select(s => new
                {
                    s.Id,
                    s.FirstName,
                    s.LastName,
                    s.Email
                })
                .FirstOrDefaultAsync();

            //CG: Obtener los cursos activos del estudiante
            var activeCourseIdsForStudent = await _context.StudentsCourses
                .Where(sc => sc.StudentId == studentId && courseIds.Contains(sc.CourseId) && sc.IsActive)
                .Select(sc => sc.CourseId)
                .ToListAsync();

            //CG: Crear una instancia de DashboardHomeStudentDto
            var studentDto = new DashboardHomeStudentDto
            {
                Id = studentDetails.Id,
                FullName = $"{studentDetails.FirstName} {studentDetails.LastName}",
                Email = studentDetails.Email,
                CoursesCount = activeCourseIdsForStudent.Count,
                PendingsCount = pendingCounts[studentId]
            };

            //CG: Agregar a la lista de estudiantes
            studentsDto.Add(studentDto);
        }

        // --------------------------- Creacion del List<DashboardHomeStudentDto> --------------------------

        var dashboardHomeDto = new DashboardHomeDto
        {
            CentersCount = centersCount,
            CoursesCount = coursesCount,
            StudentsCount = studentsCount,
            PendingActivities = pendingActivitiesDto,
            UpcomingActivities = upcomingActivitiesDto,
            Centers = centersDto,
            ActiveCourses = activeCoursesDto,
            Students = studentsDto,
        };

        return new ResponseDto<DashboardHomeDto>
        {
            StatusCode = 200,
            Status = true,
            Message = MessagesConstant.RECORDS_FOUND,
            Data = dashboardHomeDto
        };
    }
}
