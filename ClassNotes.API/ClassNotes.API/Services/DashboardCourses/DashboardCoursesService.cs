using AutoMapper;
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

        public DashboardCoursesService(
            ClassNotesContext context,
            IMapper mapper,
            IAuditService auditService
        )
        {
            _context = context;
            _mapper = mapper;
            _auditService = auditService;
        }

        // CP -> Mostrar el dashboard de un curso
        public async Task<ResponseDto<DashboardCourseDto>> GetDashboardCourseAsync(Guid courseId) // Como parametro lleva el id del curso que se desea ver
        {
            var userId = _auditService.GetUserId(); // Obtener el ID del usuario que hace la petición

            // Verificar que el curso exista y pertenezca al usuario
            var course = await _context.Courses
                .Include(c => c.CreatedByUser) // Creador del curso
                .FirstOrDefaultAsync(c => c.Id == courseId);

            // Si el curso no existe
            if (course == null)
            {
                return new ResponseDto<DashboardCourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }

            // Si el curso no pertenece al usuario
            if (course.CreatedByUser.Id != userId)
            {
                return new ResponseDto<DashboardCourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }

            // Contar estudiantes del curso
            var studentsCount = await _context.StudentsCourses
                .Where(sc => sc.CourseId == courseId) // Solo filtramos por el curso
                .CountAsync();

            // Contar actividades pendientes del curso
            var pendingActivitiesCount = await _context.Activities
                .Where(a => a.Unit.CourseId == courseId && a.QualificationDate > DateTime.UtcNow) // Si las actividades tienen fecha futura son actividades pendientes, sino significa que ya fueron evaluadas
                .CountAsync();

            // Obtener estudiantes del curso
            var students = await _context.StudentsCourses
                .Where(sc => sc.CourseId == courseId) // Solo filtramos por el curso
                .OrderBy(sc => sc.Student.FirstName) // Ordenar alfabeticamente
                .Include(sc => sc.Student)
                .Take(5) // Para mostrar unicamente 5
                .ToListAsync();

            // Obtener actividades del curso
            var activities = await _context.Activities
                .Where(a => a.Unit.CourseId == courseId) // Filtrar por curso
                .OrderByDescending(a => a.QualificationDate) // Ordenar por fecha de calificación
                .Take(5) // Para mostrar unicamente 5
                .ToListAsync();

            // Calcular el puntaje evaluado
            var scoreEvaluated = await CalculateScoreEvaluated(courseId);

            // Mapear los estudiantes a DTOs
            var studentsDto = _mapper.Map<List<DashboardCourseStudentDto>>(
                students.Select(sc => sc.Student)
            );

            // Mapear las actividades a DTOs
            var activitiesDto = _mapper.Map<List<DashboardCourseActivityDto>>(activities);

            // Construir el objeto de respuesta
            var dashboardCourseDto = new DashboardCourseDto
            {
                StudentsCount = studentsCount,
                PendingActivitiesCount = pendingActivitiesCount,
                ScoreEvaluated = scoreEvaluated,
                Activities = activitiesDto,
                Students = studentsDto
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