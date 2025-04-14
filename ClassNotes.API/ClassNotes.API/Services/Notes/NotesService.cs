using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Notes.QualificationDasboard;
using ClassNotes.API.Dtos.Notes;
using Serilog;


namespace ClassNotes.API.Services.Notes
{
	public class NotesService : INotesService
	{
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<NotesService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ClassNotesContext _context;
        private readonly int PAGE_SIZE;

        public NotesService(ClassNotesContext context,
            IMapper mapper,
			IAuditService auditService,
            ILogger<NotesService> logger,
			IConfiguration configuration) 
		{
            this._mapper = mapper;
            this._auditService = auditService;
            this._logger = logger;
            this._configuration = configuration;
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Students");
            this._context = context;
        }


        public async Task<ResponseDto<PaginationDto<List<StudentUnitNoteDto>>>> GetStudentUnitsNotesAsync(Guid studentId, Guid courseId, int page = 1)
        {

            int startIndex = (page - 1) * PAGE_SIZE;

            var userId = _auditService.GetUserId();

            //Busca todas las entidades de unidad estudiante
            var studentUnitsQuery = _context.StudentsUnits
                    .Include(c => c.StudentCourse)
                    .Where(c=>  c.StudentCourse.StudentId == studentId && c.StudentCourse.CourseId == courseId && c.CreatedBy==userId) 
                    .AsQueryable();

            var courseEntity = _context.Courses.Include(x => x.CourseSetting).FirstOrDefault(x => x.Id ==courseId);

            int totalItems = await studentUnitsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

          
            var studentUnitEntities = await studentUnitsQuery
                .OrderBy(n => n.UnitNumber) //Filtrar en orden...
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var studentUnitDto = _mapper.Map<List<StudentUnitNoteDto>>(studentUnitEntities);

            return new ResponseDto<PaginationDto<List<StudentUnitNoteDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<StudentUnitNoteDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = studentUnitDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        public async Task<ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>> GetStudentsNotesAsync( Guid courseId, int page = 1)
        {

            int startIndex = (page - 1) * PAGE_SIZE;

            var userId = _auditService.GetUserId();

            //Obtiene el curso del que se quieren ver las notas de todos sus estudiantes
            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == courseId  );

            if (course is null)
            {
                return new ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>
                {
                    StatusCode = 401,
                    Status = false,
                    Message = "El curso No Existe"
                };
            }
            if (course.CreatedBy != userId)
            {
                return new ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>
                {
                    StatusCode = 401,
                    Status = false,
                    Message = "No esta autorizado para ver estos registros."
                };
            }

     
            //Busca todas las relaciones entre cursos y estudiantes, asi se obtienen solo estudiantes del curso...
            var studentCoursesQuery = _context.StudentsCourses
                    .Where(c => c.CourseId == courseId)
                    .AsQueryable();


            //paginacion
            int totalItems = await studentCoursesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);
            var studentNotesEntities = await studentCoursesQuery
                .OrderByDescending(n => n.CreatedDate)
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();


            //Estos dto seran los que reciba el usuario, incluyen la nota no ponderada del alumno y la ponderada... 
            var studentNoteDto = _mapper.Map<List<StudentTotalNoteDto>>(studentNotesEntities);


            //Por cada estudiante, la nota final ya esta almacenada en la entidad studentCourse ...
             studentNoteDto.ForEach(studentNote => {

                 //Si la nota propedada es mayor a 100, seguarda como 100 en el dto, sino, se retorna directamente...
                 if(studentNote.FinalNote > 100)
                 {
                     studentNote.FinalNote = 100;
                 }

             });


            return new ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<StudentTotalNoteDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = studentNoteDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        public async Task<ResponseDto<PaginationDto<List<StudentActivityNoteDto>>>> GetStudentsActivitiesAsync(Guid courseId, int page = 1)
        {

            int startIndex = (page - 1) * PAGE_SIZE;

            var userId = _auditService.GetUserId();

            var studentctivitesQuery = _context.StudentsActivitiesNotes
                .Include(x => x.Activity)
                .ThenInclude(u => u.Unit).AsQueryable()
                    .Where(c => c.Activity.Unit.CourseId == courseId && c.CreatedBy == userId);

            var activities = _context.Activities.Include(u=> u.Unit).Where(x=> x.Unit.CourseId==courseId && x.CreatedBy==userId);

            int totalItems = await studentctivitesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);


            var studentNotesEntities = await studentctivitesQuery
                .OrderByDescending(n => n.CreatedDate)
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var studentNoteDto = _mapper.Map<List<StudentActivityNoteDto>>(studentNotesEntities);

            //busca la actividad relacionada con la actividad revisada del estudiante para indicar si es extra o no la que se reviso.
            studentNoteDto.ForEach(x =>
            {
                var activity = activities.FirstOrDefault(u => u.Id == x.ActivityId);
                x.IsExtra = activity.IsExtra;
                x.Note = (x.Note / 100) * activity.MaxScore;
            });
     

            return new ResponseDto<PaginationDto<List<StudentActivityNoteDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<StudentActivityNoteDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = studentNoteDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }






        /// <summary>
        /// En este endpoit se trabaja lo que es el conjunto de datos de vista de calificaciones generales de un estudiante
        /// En el cual retornarmos estadisticas generales, estadisticas de promedios, estadisticas de los estudiantes y sus notas generales segun las unidades de los mismos
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="activeStudent"></param>
        /// <param name="studentStateNote"></param>
        /// <param name="includeStats"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        /// <remarks>
        ///     En un momento utilize 0f , esto me dio cierto error en comparacion 1, por que es tipo flotante y la solucion que me dio VS fue 0f
        ///     0F es 0.0 , retorna un 0 pero de tipo flotante
        ///     <seealso href="https://discussions.unity.com/t/what-means-the-f-in-0-0f/453024/2"/>
        ///     <seealso href="https://stackoverflow.com/questions/5199338/what-is-the-significance-of-0-0f-when-initializing-in-c"/>
        /// </remarks>
        /// 
        public async Task<ResponseDto<DasboardRequestDto>> GetDashboardQualifications(
            Guid courseId,
            string activeStudent = null,
            string studentStateNote = null,
            bool includeStats = true,
            int page = 1,
            int pageSize = 10,
            string searchTerm = "")
        {
            try
            {
                // Validación de parámetros
                var validationResult = ValidateParameters(activeStudent, studentStateNote);
                if (validationResult != null)
                    return validationResult;

                // Obtener configuración del curso
                var course = await _context.Courses
                    .Include(c => c.CourseSetting)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                    return new ResponseDto<DasboardRequestDto>
                    {
                        StatusCode = 404,
                        Status = false,
                        Message = "Curso no encontrado",
                        Data = null
                    };

                //  Obtener tipo de puntuación del curso
                // si no se asigna aritmetico y listo
                var scoreType = course.CourseSetting?.ScoreType ?? ScoreTypeConstant.ARITHMETI_SCORE;

                
                //  Obtener datos de estudiantes y notas
                var studentData = await GetStudentQualificationsData(
                    courseId,
                    activeStudent?.ToUpper(),
                    scoreType,
                    page,
                    pageSize,
                    searchTerm,
                    studentStateNote
                    );

                // Calcular estadísticas si es necesario
                // si el FE no lo pide pasa de largo
                var statistics = new StadisticStudentsDto();
                if (includeStats)
                {
                   var  allStudents = await GetStudentQualificationsData(
                        courseId,
                        activeStudent?.ToUpper(),
                        scoreType,
                        null,
                        null,
                        searchTerm,
                        studentStateNote);
                    statistics = await CalculateCourseStatistics(courseId, scoreType, allStudents.Items);
                }



                return new ResponseDto<DasboardRequestDto>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "Datos obtenidos correctamente",
                    Data = new DasboardRequestDto
                    {
                        StadisticStudents = statistics,
                        StudentQualifications = studentData
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener calificaciones del dashboard");
                return new ResponseDto<DasboardRequestDto>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = "Error interno del servidor",
                    Data = null
                };
            }
        }

        private async Task<StadisticStudentsDto> CalculateCourseStatistics(
            Guid courseId,
            string scoreType,
            List<StudentQualificationDto> studentData
            )
        {
            // Obtener promedios por unidad
            var unitAverages = new List<UnitStatus>();

            var units = await _context.Units
                .Where(u => u.CourseId == courseId)
                .ToListAsync();

            foreach (var unit in units)
            {
                var notesQuery = from note in _context.StudentsActivitiesNotes
                                 join activity in _context.Activities on note.ActivityId equals activity.Id
                                 where activity.UnitId == unit.Id
                                 group new { note, activity } by activity.UnitId into g
                                 select new
                                 {
                                     SumNotes = g.Sum(x => x.note.Note),
                                     SumMaxScores = g.Sum(x => x.activity.MaxScore)
                                 };

                var sums = await notesQuery.FirstOrDefaultAsync();
                float average = 0f;

                if (sums != null)
                {
                    if (scoreType == ScoreTypeConstant.GOLD_SCORE)
                        average = sums.SumNotes;
                    if (scoreType == ScoreTypeConstant.ARITHMETI_SCORE && sums.SumMaxScores > 0)
                        average = (sums.SumNotes / sums.SumMaxScores) * 100;
                    if (scoreType == ScoreTypeConstant.WEIGHTED_SCORE && sums.SumMaxScores > 0 && unit.MaxScore.HasValue)
                        average = (sums.SumNotes * unit.MaxScore.Value) / (sums.SumMaxScores * unit.MaxScore.Value) * 100;
                }

                unitAverages.Add(new UnitStatus
                {
                    UnitId = unit.Id,
                    UnitNumber = unit.UnitNumber,
                    Avarage = average
                });
            }

            //  Calcular estadísticas globales del curso
            float overallAverage = 0f;
            float approvalRating = 0f;

            int excellent = 0;
            int good = 0;
            int stablish = 0;
            int low = 0;
            int failed = 0;
            var courseScores = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new
                {
                    c.CourseSetting.MaximumGrade,
                    c.CourseSetting.MinimumGrade
                })
                .FirstOrDefaultAsync();
            if (studentData.Any())
            {
                overallAverage = studentData.Average(s => s.GlobalAverage);

                int approvedCount = studentData.Count(s => s.GlobalAverage >= courseScores.MaximumGrade);
                approvalRating = (float)approvedCount / studentData.Count * 100;

                // Clasificar estudiantes por categoría
                foreach (var student in studentData)
                {
                    var avg = student.GlobalAverage;

                    switch (avg)
                    {
                        case >= 90:
                            excellent++;
                            break;
                        case >= 80:
                            good++;
                            break;
                        case >= 70:
                            stablish++;
                            break;
                        case >= 60:
                            low++;
                            break;
                        default:
                            failed++;
                            break;
                    }
                }
            }
            //  Identificar la mejor y peor unidad
            UnitStatus bestUnit = null;
            UnitStatus worstUnit = null;

            if (unitAverages.Any())
            {
                var orderedUnits = unitAverages.OrderByDescending(u => u.Avarage).ToList();
                bestUnit = orderedUnits.First();
                worstUnit = orderedUnits.Last();

                // Manejar caso donde todas las unidades tienen el mismo promedio
                if (bestUnit.Avarage == worstUnit.Avarage)
                {
                    worstUnit = null;// no mostrar peor unidad si son iguales
                }
            }
            var graficResult = new GraficResultDTO
            {
                BigTotal = studentData.Count,
                ExcellentTotal = excellent,
                GoodTotal = good,
                StablishTotal = stablish,
                LowTotal = low,
                FailedTotal = failed
            };
            // Retornar el DTO con todas las estadísticas
            return new StadisticStudentsDto
            {
                OverallAvarage = overallAverage,
                ApprovalRating = approvalRating,
                BestUnit = bestUnit,
                WorstUnit = worstUnit,
                GraficResult = graficResult // Estadisticas del grafico
                
            };
        }

        private async Task<PaginationDto<List<StudentQualificationDto>>> GetStudentQualificationsData(
            Guid courseId,
            string activeFilter,
            string scoreType,
            int? page,
            int? pageSize,
            string searchTerm,
            string studentStateNote = null)
        {
            //  Consulta base de estudiantes
            var query = _context.StudentsCourses
                .Include(sc => sc.Student)
                .Where(sc => sc.CourseId == courseId);

            //  Aplicar filtro de estado
            if (activeFilter == "ACTIVE")
                query = query.Where(sc => sc.IsActive);
            if (activeFilter == "INACTIVE")
                query = query.Where(sc => !sc.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(sc =>
                    sc.Student.FirstName.Contains(searchTerm) ||
                    sc.Student.LastName.Contains(searchTerm) ||
                    sc.Student.Email.Contains(searchTerm));
            }

            // Configuración del curso (nota máxima y mínima)
            var courseScores = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new
                {
                    c.CourseSetting.MaximumGrade,
                    c.CourseSetting.MinimumGrade
                })
                .FirstOrDefaultAsync();

            var studentCourses = await query.ToListAsync();
            //  Construir lista de resultados
            var result = new List<StudentQualificationDto>();

            foreach (var studentCourse in studentCourses)
            {
                var studentNotes = await GetStudentNotes(studentCourse.StudentId, courseId, scoreType);
                var globalAverage = studentNotes.Any()
                    ? studentNotes.Average(sn => sn.Note)
                    : 0;

                var stateNote = GetNoteState(globalAverage, courseScores.MaximumGrade, courseScores.MinimumGrade);

                result.Add(new StudentQualificationDto
                {
                    StudentId = studentCourse.StudentId,
                    StudentName = $"{studentCourse.Student.FirstName} {studentCourse.Student.LastName}",
                    StudentEmail = studentCourse.Student.Email,
                    StudentUnits = studentNotes,
                    GlobalAverage = globalAverage,
                    StateNote = stateNote
                });
            }

            // Aplicar filtro por estado de nota si se solicita
            if (!string.IsNullOrEmpty(studentStateNote))
            {
                result = result.Where(r => r.StateNote == studentStateNote.ToUpper()).ToList();
            }

            if (page == null || pageSize == null)
            {
                return new PaginationDto<List<StudentQualificationDto>>
                {
                    CurrentPage = 1,
                    PageSize = result.Count,
                    TotalItems = result.Count,
                    TotalPages = 1,
                    HasPreviousPage = false,
                    HasNextPage = false,
                    Items = result
                };
            }

            // No hay null 
            var totalItems = result.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize.Value);
            var pagedResult = result
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            return new PaginationDto<List<StudentQualificationDto>>
            {
                CurrentPage = page.Value,
                PageSize = pageSize.Value,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages,
                Items = pagedResult
            };

        }

        private async Task<List<StudentUnitNote>> GetStudentNotes(
            Guid studentId,
            Guid courseId,
            string scoreType)
        {
            var query =
                from note in _context.StudentsActivitiesNotes //Relacionamos con las actividades
                join activity in _context.Activities on note.ActivityId equals activity.Id
                join unit in _context.Units on activity.UnitId equals unit.Id //Relacionamos con las unidades
                where unit.CourseId == courseId && note.StudentId == studentId
                // Agrupamiento por unidad
                group new { note, activity, unit } by new { unit.Id, unit.UnitNumber } into g

                // Calculamos la nota según el tipo
                select new StudentUnitNote
                {
                    UnitID = g.Key.Id,
                    UnitNumber = g.Key.UnitNumber,
                    Note = CalculateUnitScore(
                        scoreType,
                        g.Sum(x => x.note.Note),
                        g.Sum(x => x.activity.MaxScore),
                        g.First().unit.MaxScore ?? 0)
                };



            return await query.ToListAsync();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scoreType"></param>
        /// <param name="sumNotes"></param>
        /// <param name="sumMaxScores"></param>
        /// <param name="unitWeight"></param>
        /// <returns></returns>
        /// <remarks>
        ///     el uso del switch es por que retorno directamente es un switch expression, es una manera mas corta de usarlo
        ///     <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression"/>
        /// </remarks>
        private static float CalculateUnitScore(
            string scoreType, 
            float sumNotes,
            float sumMaxScores,
            float unitWeight)
        {
            return scoreType switch
            {
                ScoreTypeConstant.GOLD_SCORE => sumNotes,
                ScoreTypeConstant.ARITHMETI_SCORE => (sumNotes / sumMaxScores) * 100,
                ScoreTypeConstant.WEIGHTED_SCORE => (sumNotes * unitWeight) / (sumMaxScores * unitWeight) * 100,
                _ => 0f
            };
        }
        /// <summary>
        /// Determina el estado de calificación de un estudiante basado en su promedio,
        /// considerando los rangos configurados por el profesor para el curso.
        /// 
        /// Este cálculo es dinámico y se adapta a la configuración particular de cada curso:
        /// - Los porcentajes de evaluación (excelente, bueno, bajo) se calculan como porcentajes
        ///   del rango entre la nota mínima y máxima configurada para el curso
        /// - Permite a cada profesor definir sus propios criterios de evaluación
        /// </summary>
        /// <param name="average">Promedio actual del estudiante</param>
        /// <param name="maxScore">Puntaje máximo configurado para el curso</param>
        /// <param name="minScore">Puntaje mínimo configurado para el curso</param>
        /// <returns>
        /// Cadena que indica el estado de la calificación:
        /// - "EXCELLENT" (Excelente): 90-100% del rango
        /// - "GOOD" (Bueno): 70-89% del rango
        /// - "LOW" (Bajo): 50-69% del rango
        /// - "FAILED" (Reprobado): Por debajo del 50% del rango
        /// </returns>
        /// <example>
        /// <code>
        /// // Ejemplo con rango 50-100:
        /// GetNoteState(95, 100, 50); // "EXCELLENT" (95 está en el 90% del rango 50-100)
        /// GetNoteState(60, 100, 50); // "LOW" (60 está en el 20% del rango 50-100)
        /// 
        /// // Ejemplo con rango 65-100:
        /// GetNoteState(90, 100, 65); // "GOOD" (90 está en ~71% del rango 65-100)
        /// </code>
        /// </example>
        /// 
        /// <seealso href="https://stackoverflow.com/questions/2683442/where-can-i-find-the-clamp-function-in-net"/>
        /// <seealso href="https://davecallan.com/csharp-use-math-clamp-force-number-between-particular-range/"/>
        private string GetNoteState(float average, float maxScore, float minScore)
        {
            // Validación básica
            if (maxScore <= minScore || average < minScore)
                return "FAILED";

            // Calculamos el porcentaje normalizado
            float percentage = (average - minScore) / (maxScore - minScore) * 100;

            // rango valido 
            percentage = Math.Clamp(percentage, 0f, 100f);

            return percentage switch
            {
                >= 90f => "EXCELLENT",  // 90-100%
                >= 80f => "GOOD",       // 80-89%
                >= 70f => "STABLISH",   // 70-79%
                >= 60f => "LOW",        // 60-69%
                _ => "FAILED"           // 0-49%
            };
        }

        private List<StudentQualificationDto> FilterByNoteState(
            List<StudentQualificationDto> students,
            string state)
        {
            return state switch
            {
                "EXCELLENT" => students.Where(s => s.StateNote == "EXCELLENT").ToList(),
                "GOOD" => students.Where(s => s.StateNote == "GOOD").ToList(),
                "LOW" => students.Where(s => s.StateNote == "LOW").ToList(),
                "FAILED" => students.Where(s => s.StateNote == "FAILED").ToList(),
                _ => students
            };
        }
       
        private ResponseDto<DasboardRequestDto> ValidateParameters(string activeStudent, string studentStateNote)
        {
            var activeSet = new HashSet<string> { null, "ACTIVE", "INACTIVE" };
            var noteSet = new HashSet<string> { null, "EXCELLENT", "GOOD", "LOW", "FAILED" };

            if (activeStudent != null && !activeSet.Contains(activeStudent.ToUpper()))
            {
                return new ResponseDto<DasboardRequestDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Active Students solo puede ser [ALL, ACTIVE, INACTIVE]",
                    Data = null
                };
            }

            if (studentStateNote != null && !noteSet.Contains(studentStateNote.ToUpper()))
            {
                return new ResponseDto<DasboardRequestDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "studentStateNote solo puede ser [ALL, EXCELLENT, GOOD, LOW, FAILED]",
                    Data = null
                };
            }

            return null;
        }

    }
}
