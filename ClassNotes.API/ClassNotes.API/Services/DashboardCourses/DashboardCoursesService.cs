using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.DashboardCourses;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.DashboardCourses
{
    // --------------------- CP --------------------- //
    public class DashboardCoursesService : IDashboardCoursesService
    {
        private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;

        // para problemas de multiples hilos en la colsulat de get all, 
        // HR -> me lo dio chatGPT no estoy seguro de por que sucede
        // lo poco que entendi es qu el Dbcontext se queda como con instancias o ago asi
        private readonly IServiceScopeFactory _scopeFactory;

        public DashboardCoursesService(
            ClassNotesContext context,
            IMapper mapper,
            IAuditService auditService,
            IServiceScopeFactory scopeFactory
        )
        {
            _context = context;
            _mapper = mapper;
            _auditService = auditService;
            _scopeFactory = scopeFactory;
        }

        // CP -> Mostrar el dashboard de un curso
         // HR ->Ajustes para FE
         //  TODO : LOGICA DEL RESULTADO VERDADERAMENTE EVALUADO SEGUN EL SETTING 
        public async Task<ResponseDto<DashboardCourseDto>> GetDashboardCourseAsync(Guid courseId) // Como parametro lleva el id del curso que se desea ver
        {
            var userId = _auditService.GetUserId(); // Obtener el ID del usuario que hace la petición
            // Validar si el curso existe y pertenece al usuario


            // donde se vea el _scopeFactory es para manejar lo de multiples hilos
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                // Verificar que el curso exista y pertenezca al usuario
                // HR lo convine en una sola consulta para verificar los mismos
                //  Consulta combinada para validación
                var courseExists = await context.Courses
                    .AnyAsync(c => c.Id == courseId && c.CreatedByUser.Id == userId);

                if (!courseExists)
                {
                    return new ResponseDto<DashboardCourseDto>
                    {
                        StatusCode = 404,
                        Status = false,
                        Message = MessagesConstant.RECORD_NOT_FOUND
                    };
                }
            }

            // Crear nuevas tareas con diferentes instancias de `DbContext`
            // HR
            // Conteo de los estudiantes
            var studentsCountTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await context.StudentsCourses.CountAsync(sc => sc.CourseId == courseId);
            });
            // conteo de las actividades pendientes 
            // 
            var pendingActivitiesCountTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await context.Activities.CountAsync(a => a.Unit.CourseId == courseId && a.QualificationDate > DateTime.UtcNow);
            });


            // Contar el numero de Notas pendientes (recordatorios que tiene el docente)
            // Cuenta todas las notas pendientes si su fecha de uso es mayor a la de ayer o su su esta es no visto(marcado como no visto) 
            var pendingNotesTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await context.CoursesNotes.CountAsync(n => n.CourseId == courseId && (!n.isView || n.UseDate > DateTime.UtcNow.AddDays(-1)));
            });

            // el total evaluado por el docente en actividades
            var scoreEvaluatedTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await CalculateScoreEvaluated(courseId);
            });

            // HR
            //Preparacion de consultas combinadas 
            //  https://stackoverflow.com/questions/12211680/what-difference-does-asnotracking-make
            // el AsNotracking es por que no esperamos actualizarlas en ningun momento en si solo son vistas y ya no se pueden editar

            // Los estudiantes que se muestran en el Dashboard
            var studentsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await context.StudentsCourses
                    .Where(sc => sc.CourseId == courseId)
                    .OrderBy(sc => sc.Student.FirstName)
                    .Take(5)
                    .Select(sc => sc.Student)
                    .ProjectTo<DashboardCourseStudentDto>(_mapper.ConfigurationProvider)
                    .AsNoTracking()
                    .ToListAsync();
            });
            // Las actividades que se muestran en el dashboard
            var activitiesTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await context.Activities
                    .Where(a => a.Unit.CourseId == courseId)
                    .OrderByDescending(a => a.QualificationDate)
                    .Take(5)
                    .ProjectTo<DashboardCourseActivityDto>(_mapper.ConfigurationProvider)
                    .AsNoTracking()
                    .ToListAsync();
            });

            //Maximo que se puede tener en el curos
            var maxScoreTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ClassNotesContext>();
                return await context.Courses
                    .Where(c => c.Id == courseId)
                    .Select(c => c.CourseSetting.MaximumGrade)
                    .FirstOrDefaultAsync();
            });

            // Esperar a que todas las tareas se completen
            await Task.WhenAll(
                studentsCountTask,
                pendingActivitiesCountTask,
                pendingNotesTask,
                scoreEvaluatedTask,
                studentsTask,
                activitiesTask,
                maxScoreTask
            );

            // Crear el objeto de respuesta
            var dashboardCourseDto = new DashboardCourseDto
            {
                StudentsCount = studentsCountTask.Result,
                PendingActivitiesCount = pendingActivitiesCountTask.Result,
                ScoreEvaluated = scoreEvaluatedTask.Result,
                PendingNotesRemenbers = pendingNotesTask.Result,
                Activities = activitiesTask.Result,
                MaxScoreEvaluated = maxScoreTask.Result,
                Students = studentsTask.Result
            };

            return new ResponseDto<DashboardCourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = dashboardCourseDto
            };
        }


        // CP -> Calcular el puntaje evaluado
        private async Task<float> CalculateScoreEvaluated(Guid courseId)
        {
            // Obtener las actividades del curso
            var activities = await _context.Activities
                .Where(a => a.Unit.CourseId == courseId) // Filtrar por curso
                .Include(a => a.Unit) // Se incluye la unidad para acceder a su nota maxima
                .ToListAsync();

            // Si no hay actividades, retornar 0
            if (activities == null || !activities.Any())
            {
                return 0;
            }

            // Calcular la suma de los valores maximos de todas las actividades
            float totalMaxScores = activities.Sum(a => a.MaxScore);

            // Calcular el puntaje evaluado ponderado
            float totalScoreEvaluated = 0;

            foreach (var activity in activities)
            {
                // Verificar si la actividad ya fue evaluada (QualificationDate <= DateTime.UtcNow)
                if (activity.QualificationDate <= DateTime.UtcNow)
                {
                    // Calcular el ponderado de esta actividad
                    if (totalMaxScores > 0 && activity.Unit.MaxScore > 0)
                    {
                        float weightedScore = (activity.MaxScore / totalMaxScores) * activity.Unit.MaxScore; // Aqui se hace la ponderación
                        totalScoreEvaluated += weightedScore;
                    }
                }
            }

            // Retornar el total de puntos evaluados
            return (float)Math.Round(totalScoreEvaluated, 2);
        }
    }
}