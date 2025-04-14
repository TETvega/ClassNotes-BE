using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Attendances.Student;

namespace ClassNotes.API.Services.Attendances
{
    public class AttendancesService : IAttendancesService
	{
		private readonly ClassNotesContext _context;
		private readonly IAuditService _auditService;
		private readonly int PAGE_SIZE;

		public AttendancesService(ClassNotesContext context, IAuditService auditService, IConfiguration configuration)
		{
			this._context = context;
			this._auditService = auditService;
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
                .Select(s => new { FullName = s.FirstName + ' ' + s.LastName })
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
                StudentName = student?.FullName ?? "Desconocido",
                AttendanceCount = attendedCount,
                AttendanceRate = attendanceRate,
                AbsenceCount = absenceCount,
                AbsenceRate = absenceRate,
                IsActive = attendedCount > 0
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
        public async Task<ResponseDto<PaginationDto<List<AttendanceDto>>>> GetAttendancesByStudentPaginationAsync(StudentIdCourseIdDto dto, string searchTerm = "", int page = 1, bool isCurrentMonth = false, int pageSize = 10)
        {
            int startIndex = (page - 1) * pageSize;

            //JA: Validar existencia del estudiante en el curso
            var studentCourse = await _context.StudentsCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == dto.StudentId && sc.CourseId == dto.CourseId);
            if (studentCourse == null)
            {
                return new ResponseDto<PaginationDto<List<AttendanceDto>>>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ATT_STUDENT_NOT_ENROLLED,
                };
            }

            //JA: Consultar asistencias del estudiante en el curso
            var query = _context.Attendances
                .Where(a => a.StudentId == dto.StudentId && a.CourseId == dto.CourseId);

            //JA: Filtrar asistencia por fecha específica (si se proporciona un término de búsqueda)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a => a.RegistrationDate.ToString().Contains(searchTerm));
            }

            //JA: Filtrar por mes actual si isCurrentMonth es true
            if (isCurrentMonth)
            {
                var currentMonth = DateTime.Now.Month;
                query = query.Where(a => a.RegistrationDate.Month == currentMonth);
            }

            //JA: Contar el total de registros de asistencia encontrados
            int totalItems = await query.CountAsync();

            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var attendances = await query
                .OrderByDescending(a => a.RegistrationDate)
                .Skip(startIndex)
                .Take(pageSize)
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    Attended = a.Attended,
                    RegistrationDate = a.RegistrationDate,
                    CourseId = a.CourseId,
                    StudentId = a.StudentId
                })
                .ToListAsync();

            return new ResponseDto<PaginationDto<List<AttendanceDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ATT_RECORDS_FOUND,
                Data = new PaginationDto<List<AttendanceDto>>
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

        public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto attendanceCreateDto)
        {
            // Validar que el curso existe
            var course = await _context.Courses
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.Id == attendanceCreateDto.CourseId);

            if (course == null)
            {
                throw new ArgumentException("El curso no existe.");
            }

            // Validar que el estudiante existe
            var student = await _context.Students.FindAsync(attendanceCreateDto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("El estudiante no existe.");
            }

            // Validar que el profesor existe
            var teacher = await _context.Users.FindAsync(attendanceCreateDto.TeacherId);
            if (teacher == null)
            {
                throw new ArgumentException("El profesor no existe.");
            }

            // Validar que el profesor pertenece al centro del curso
            var center = await _context.Centers
                .FirstOrDefaultAsync(c => c.Id == course.CenterId && c.TeacherId == attendanceCreateDto.TeacherId);

            if (center == null)
            {
                throw new ArgumentException("El profesor no está asignado a este centro.");
            }

            // Crear la asistencia
            var attendance = new AttendanceEntity
            {
                Attended = attendanceCreateDto.Attended,
                Status = attendanceCreateDto.Status,
                RegistrationDate = attendanceCreateDto.RegistrationDate,
                CourseId = attendanceCreateDto.CourseId,
                StudentId = attendanceCreateDto.StudentId,
                CreatedByUser = teacher,  // Asignamos el objeto UserEntity completo
                UpdatedByUser = teacher   // Asignamos el objeto UserEntity completo
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                Status = attendance.Status,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }

        public async Task<AttendanceDto> EditAttendanceAsync(Guid attendanceId, bool attended)
        {
            var attendance = await _context.Attendances
                .Include(a => a.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                throw new ArgumentException("La asistencia no existe.");
            }

            attendance.Attended = attended;

            // No podemos actualizar UpdatedDate directamente ya que no existe en la entidad
            // Pero podemos actualizar UpdatedByUser si es necesario
            // attendance.UpdatedByUser = ...;

            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                Status = attendance.Status,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }

        public async Task<AttendanceDto> EditAttendanceAsync(Guid attendanceId, AttendanceEditDto attendanceEditDto)
        {
            // Obtener la asistencia existente
            var attendance = await _context.Attendances
                .Include(a => a.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                throw new ArgumentException("La asistencia no existe.");
            }

            // Actualizar solo el status (sin modificar Attended si no es necesario)
            attendance.Status = attendanceEditDto.Status;

            // Guardar cambios
            await _context.SaveChangesAsync();

            // Devolver el DTO actualizado
            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended, // Mantiene el valor existente
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
                // No incluye Status si no está en el DTO
            };
        }

        public async Task<List<AttendanceDto>> ListAttendancesAsync()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Student)
                .Include(a => a.CreatedByUser)
                .Include(a => a.UpdatedByUser)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Status = a.Status,
                Attended = a.Attended,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = $"{a.Student?.FirstName} {a.Student?.LastName}"
            }).ToList();
        }

        public async Task<List<AttendanceDto>> ListAttendancesByCourseAsync(Guid courseId)
        {
            var attendances = await _context.Attendances
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Course)
                .Include(a => a.Student)
                .Include(a => a.CreatedByUser)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                Status = a.Status,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = $"{a.Student?.FirstName} {a.Student?.LastName}"
            }).ToList();
        }

        public async Task<List<AttendanceDto>> ListAttendancesByStudentAsync(Guid studentId)
        {
            var attendances = await _context.Attendances
                .Where(a => a.StudentId == studentId)
                .Include(a => a.Course)
                .Include(a => a.Student)
                .Include(a => a.CreatedByUser)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                Status = a.Status,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = $"{a.Student?.FirstName} {a.Student?.LastName}"
            }).ToList();
        }
    }
}