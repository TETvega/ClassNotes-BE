using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Dtos.Attendances.Student;
using System.Globalization;

namespace ClassNotes.API.Services.Attendances
{
    public class AttendancesService : IAttendancesService
	{
		private readonly ClassNotesContext _context;
		private readonly IAuditService _auditService;
		private readonly int PAGE_SIZE;

		public AttendancesService(ClassNotesContext context, IAuditService auditService, IConfiguration configuration)
		{
			_context = context;
			_auditService = auditService;
			PAGE_SIZE = configuration.GetValue<int>("PageSize:StudentsAttendances");
		}

		// AM: Obtener stats de las asistencias por Id del curso
		public async Task<ResponseDto<CourseAttendancesDto>> GetCourseAttendancesStatsAsync(Guid courseId)
		{
			// AM: Id del usuario en sesión
			var userId = _auditService.GetUserId();

			// AM: Validar existencia del curso
			var courseEntity = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.CreatedBy == userId);
			if (courseEntity == null)
			{
				return new ResponseDto<CourseAttendancesDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.CRS_RECORD_NOT_FOUND
				};
			}

			// AM: Obtener todas las asistencias del curso
			var attendances = await _context.Attendances.Where(a => a.CourseId == courseId).ToListAsync();

			// AM: Validación si el curso no tiene asistencias
			if (!attendances.Any())
			{
				return new ResponseDto<CourseAttendancesDto>
				{
					StatusCode = 200,
					Status = true,
					Message = MessagesConstant.ATT_RECORDS_NOT_FOUND,
					Data = new CourseAttendancesDto
					{
						AttendanceTakenDays = 0,
						AttendanceRating = 0,
						AbsenceRating = 0
					}
				};
			}

			// AM: Contar días en los que se tomaron asistencias (RegistrationDate)
			var attendanceTakenDays = attendances
				.Select(a => a.RegistrationDate.Date)
				.Distinct()
				.Count();

			// AM: Calcular tasa de asistencias (Attended = true)
			var totalAttendances = attendances.Count;
			var attendedCount = attendances.Count(a => a.Attended);
			var attendanceRating = (double)attendedCount / totalAttendances;

			// AM: Calcular tasa de ausencias (Attended = false)
			var absenceRating = 1 - attendanceRating;

			// AM: Redondear a 2 decimales
			attendanceRating = Math.Round(attendanceRating, 2);
			absenceRating = Math.Round(absenceRating, 2);

			var statsDto = new CourseAttendancesDto
			{
				AttendanceTakenDays = attendanceTakenDays,
				AttendanceRating = attendanceRating,
				AbsenceRating = absenceRating
			};

			return new ResponseDto<CourseAttendancesDto>
			{
				StatusCode = 200,
				Status = true,
				Message = MessagesConstant.ATT_RECORDS_FOUND,
				Data = statsDto
			};
		}

		// AM: Mostrar paginación de estudiantes por Id del curso
		public async Task<ResponseDto<PaginationDto<List<CourseAttendancesStudentDto>>>> GetStudentsAttendancesPaginationAsync(Guid courseId, bool? isActive = null, string searchTerm = "", int page = 1,int? pageSize= null)
		{

            int currentPageSize = pageSize == -1 ? int.MaxValue : Math.Max(1, pageSize ?? PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;

            // AM: ID del usuario en sesión
            var userId = _auditService.GetUserId();

			// AM: Validar existencia del curso
			var courseEntity = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.CreatedBy == userId);
			if (courseEntity == null)
			{
				return new ResponseDto<PaginationDto<List<CourseAttendancesStudentDto>>>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.CRS_RECORD_NOT_FOUND,
				};
			}

			// AM: Obtener los estudiantes del curso
			var studentsInCourse = _context.StudentsCourses
				.Where(sc => sc.CourseId == courseId) // AM: Filtrar por el id del curso
				.Join(_context.Students, // AM: Unir con la tabla Students
					sc => sc.StudentId,
					s => s.Id,
					(sc, s) => new { Student = s, StudentCourse = sc }); // AM: Proyectar el resultado en un objeto

			// AM: Filtrar los estudiantes activos e inactivos si se proporciona el parametro isActive
			if (isActive.HasValue)
			{
				studentsInCourse = studentsInCourse
					.Where(sc => sc.StudentCourse.IsActive == isActive.Value);
			}

			// AM: Mapear a CourseAttendancesStudentDto y calcular la tasa de asistencia
			var studentsQuery = studentsInCourse
				.Select(s => new CourseAttendancesStudentDto
				{
					Id = s.Student.Id,
					StudentName = s.Student.FirstName + " " + s.Student.LastName,
					Email = s.Student.Email,
					AttendanceRate = _context.Attendances
						.Where(a => a.CourseId == courseId && a.StudentId == s.Student.Id) // AM: Filtrar asistencias por CourseId y StudentId
						.Average(a => (double?)(a.Attended ? 1 : 0)), // AM: Calcular la tasa de asistencia
					IsActive = s.StudentCourse.IsActive,
				});

			// AM: Buscar por nombre del estudiante
			if (!string.IsNullOrEmpty(searchTerm))
			{
				studentsQuery = studentsQuery.Where(t => t.StudentName.ToLower().Contains(searchTerm.ToLower()));
			}

			int totalItems = await studentsQuery.CountAsync();
			int totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);

			// AM: Aplicar paginacion 
			var studentsList = await studentsQuery
				.OrderBy(s => s.StudentName) // AM: Ordenar por nombre
				.Skip(startIndex)
				.Take(currentPageSize)
				.ToListAsync();

			return new ResponseDto<PaginationDto<List<CourseAttendancesStudentDto>>>
			{
				StatusCode = 200,
				Status = true,
				Message = totalItems == 0 ? MessagesConstant.STU_RECORDS_NOT_FOUND : MessagesConstant.STU_RECORDS_FOUND, // AM: Si no encuentra items mostrar el mensaje correcto
				Data = new PaginationDto<List<CourseAttendancesStudentDto>>
				{
					CurrentPage = page,
					PageSize = currentPageSize,
					TotalItems = totalItems,
					TotalPages = totalPages,
					Items = studentsList,
					HasPreviousPage = page > 1,
					HasNextPage = page < totalPages
				}
			};
		}

        // AM: Obtener stats de las asistencias por estudiante
        public async Task<ResponseDto<StudentAttendancesDto>> GetStudentAttendancesStatsAsync(StudentIdCourseIdDto dto, bool isCurrentMonth = false)
        {
            //JA: Buscar el nombre del estudiante
            var student = await _context.Students
                .Where(s => s.Id == dto.StudentId)
                .Select(s => new
                {
                    s.FirstName,
                    s.LastName,
                    s.Email,
                    IsActive = s.Courses
                        .Where(sc => sc.CourseId == dto.CourseId)
                        .Select(sc => sc.IsActive)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            var studentCourse = await _context.StudentsCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == dto.StudentId && sc.CourseId == dto.CourseId);

            if (studentCourse == null)
            {
                return new ResponseDto<StudentAttendancesDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ATT_STUDENT_NOT_ENROLLED,
                };
            }

            //JA: Obtener la lista de asistencias del estudiante en el curso, filtrando por mes si es necesario
            var query = _context.Attendances
                .Where(a => a.StudentId == dto.StudentId && a.CourseId == dto.CourseId);

            if (isCurrentMonth)
            {
                //JA: Filtrar solo las asistencias del mes actual
                var currentMonth = DateTime.Now.Month;
                query = query.Where(a => a.RegistrationDate.Month == currentMonth);
            }

            var attendances = await query.ToListAsync();

            //JA: Calcular estadísticas
            int totalAttendances = attendances.Count;
            int attendedCount = attendances.Count(a => a.Attended);
            int absenceCount = totalAttendances - attendedCount;

            //JA: Si no hay asistencias, tanto attendanceRate como absenceRate serán 0
            double attendanceRate = totalAttendances > 0 ? Math.Round((double)attendedCount / totalAttendances * 100, 2) : 0;
            double absenceRate = totalAttendances > 0 ? 100 - attendanceRate : 0; // Asegura que absenceRate sea 0 si no hay asistencias

            var studentStats = new StudentAttendancesDto
            {
                StudentFirstName = student?.FirstName ?? "Desconocido",
				StudentLastName = student?.LastName ?? "Desconocido",
				StudentEmail = student?.Email ?? "Desconocido",
                AttendanceCount = attendedCount,
                AttendanceRate = attendanceRate,
                AbsenceCount = absenceCount,
                AbsenceRate = absenceRate,
                IsActive = student.IsActive,
            };

            return new ResponseDto<StudentAttendancesDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ATT_RECORDS_FOUND,
                Data = studentStats
            };
        }

        // AM: Mostrar paginación de asistencias por estudiante
        public async Task<ResponseDto<PaginationDto<List<StudentsDATAAttendances>>>> GetAttendancesByStudentPaginationAsync(
			StudentIdCourseIdDto dto,
			string searchTerm = "",
			int page = 1,
			bool isCurrentMonth = false,
			int pageSize = 10)
        {
            int startIndex = (page - 1) * pageSize;

            // Validar existencia del estudiante en el curso
            var studentCourse = await _context.StudentsCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == dto.StudentId && sc.CourseId == dto.CourseId);
            if (studentCourse == null)
            {
                return new ResponseDto<PaginationDto<List<StudentsDATAAttendances>>>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ATT_STUDENT_NOT_ENROLLED,
                };
            }

            // Consultar asistencias del estudiante en el curso
            var query = _context.Attendances
                .Where(a => a.StudentId == dto.StudentId && a.CourseId == dto.CourseId);

            // Filtrar asistencia por fecha específica
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a => a.RegistrationDate.ToString().Contains(searchTerm));
            }

            // Filtrar por mes actual si isCurrentMonth es true
            if (isCurrentMonth)
            {
                var currentMonth = DateTime.Now.Month;
                query = query.Where(a => a.RegistrationDate.Month == currentMonth);
            }

            // Contar el total de registros de asistencia encontrados
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var cultureEs = new CultureInfo("es-ES");
            var attendances = await query
                .OrderByDescending(a => a.RegistrationDate)
                .Skip(startIndex)
                .Take(pageSize)
                .Select(a => new StudentsDATAAttendances
                {
                    Attendance = a.Attended,
                    Status = a.Status,
                    AttendaceMethod = a.Method,
                    LastChangeBy = a.ChangeBy,
                    RegisterDate = new ExtendedDateDto
                    {
                        Minute = a.RegistrationDate.ToString("mm", cultureEs),
                        Hour = a.RegistrationDate.ToString("HH", cultureEs),
                        Day = cultureEs.DateTimeFormat.GetDayName(a.RegistrationDate.DayOfWeek),
						            NumberDay =  a.RegistrationDate.ToString("dd",cultureEs),
                        Month = cultureEs.DateTimeFormat.GetMonthName(a.RegistrationDate.Month),
                        Year = a.RegistrationDate.ToString("yyyy", cultureEs)
                    }
                })
                .ToListAsync();

            return new ResponseDto<PaginationDto<List<StudentsDATAAttendances>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ATT_RECORDS_FOUND,
                Data = new PaginationDto<List<StudentsDATAAttendances>>
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = attendances,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }


    }
}