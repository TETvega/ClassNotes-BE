using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.CourseSettings;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Courses
{
    public class CoursesService : ICoursesService
    {
        private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly int PAGE_SIZE;

        public CoursesService(
            ClassNotesContext context,
            IMapper mapper,
            IAuditService auditService,
            IConfiguration configuration
        )
        {
            _context = context;
            _auditService = auditService;
            _mapper = mapper;
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Courses");
        }

        // EG -> Enlistar todos los cursos, paginacion

        public async Task<ResponseDto<PaginationDto<List<CourseWithSettingDto>>>> GetCoursesListAsync(
            string searchTerm = "",
            int page = 1,
            int? pageSize = null
        )
        {
            // Configuración del tamaño de página
            int currentPageSize = pageSize == -1 ? int.MaxValue : Math.Max(1, pageSize ?? PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;

            var userId = _auditService.GetUserId();

            // Query base con filtro por usuario e inclusión de la configuración
            var coursesQuery = _context.Courses
                .Include(c => c.CourseSetting) // Incluir la configuración asociada
                .Where(c => c.CreatedBy == userId);

            // Filtro por término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string pattern = $"%{searchTerm}%";
                coursesQuery = coursesQuery.Where(c =>
                    EF.Functions.Like(c.Name, pattern) ||
                    EF.Functions.Like(c.Code, pattern));
            }

            // Conteo total de elementos
            int totalItems = await coursesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);

            // Aplicar paginación
            var courseEntities = await coursesQuery
                .OrderByDescending(n => n.Section) // Ordenar por sección
                .Skip(startIndex)
                .Take(currentPageSize)
                .ToListAsync();

            // Mapear las entidades a CourseWithSettingDto
            var coursesWithSettingsDto = courseEntities.Select(courseEntity => new CourseWithSettingDto
            {
                Course = _mapper.Map<CourseDto>(courseEntity), // Mapear el curso
                CourseSetting = _mapper.Map<CourseSettingDto>(courseEntity.CourseSetting) // Mapear la configuración
            }).ToList();

            return new ResponseDto<PaginationDto<List<CourseWithSettingDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CRS_RECORDS_FOUND,
                Data = new PaginationDto<List<CourseWithSettingDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = coursesWithSettingsDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        // CP -> Para listar un curso mediante su id
        public async Task<ResponseDto<CourseWithSettingDto>> GetCourseByIdAsync(Guid id)
        {
            var userId = _auditService.GetUserId();

            // Incluir la relación con CourseSetting para obtener la configuración del curso
            var courseEntity = await _context.Courses
                .Include(c => c.CourseSetting) // Cargar la configuración asociada
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == userId); // Solo el creador puede ver el curso

            if (courseEntity == null)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CRS_RECORD_NOT_FOUND
                };
            }

            // Mapear la entidad a CourseWithSettingDto
            var courseWithSettingDto = new CourseWithSettingDto
            {
                Course = _mapper.Map<CourseDto>(courseEntity), // Mapear el curso
                CourseSetting = _mapper.Map<CourseSettingDto>(courseEntity.CourseSetting) // Mapear la configuración
            };

            return new ResponseDto<CourseWithSettingDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CRS_RECORD_FOUND,
                Data = courseWithSettingDto
            };
        }

        // CP -> Crear un curso
        public async Task<ResponseDto<CourseWithSettingDto>> CreateAsync(CourseWithSettingCreateDto dto)
        {
            var userId = _auditService.GetUserId();

            // Validaciones básicas del curso
            if (dto.Course.FinishTime.HasValue && dto.Course.FinishTime <= dto.Course.StartTime)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CNS_END_TIME_BEFORE_START_TIME
                };
            }

            // Validaciones de la configuración del curso
            if (dto.CourseSetting.EndDate.HasValue && dto.CourseSetting.EndDate <= dto.CourseSetting.StartDate)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CP_INVALID_DATES
                };
            }

            if (dto.CourseSetting.MinimumGrade <= 0 ||
                dto.CourseSetting.MaximumGrade <= 0 ||
                dto.CourseSetting.MaximumGrade < dto.CourseSetting.MinimumGrade)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CP_INVALID_GRADES
                };
            }

            // Verificar si ya existe una clase con el mismo nombre, código, hora de inicio y hora de finalización
            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.CreatedBy == userId &&
                    c.Name.ToLower() == dto.Course.Name.ToLower() &&
                    c.StartTime == dto.Course.StartTime &&
                    (c.FinishTime == null && dto.Course.FinishTime == null ||
                     c.FinishTime != null && dto.Course.FinishTime != null &&
                     c.FinishTime == dto.Course.FinishTime) &&
                    (c.Code == null && dto.Course.Code == null ||
                     c.Code != null && dto.Course.Code != null &&
                     c.Code.ToLower() == dto.Course.Code.ToLower()) &&
                    (c.Section == null && dto.Course.Section == null ||
                     c.Section != null && dto.Course.Section != null &&
                     c.Section.ToLower() == dto.Course.Section.ToLower())
                );

            if (existingCourse != null)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CRS_ALREADY_EXISTS
                };
            }

            // Crear o duplicar la configuración del curso
            CourseSettingEntity duplicatedSettingEntity;

            if (dto.Course.SettingId.HasValue && dto.Course.SettingId != Guid.Empty) // Caso 1: Duplicar una configuración existente
            {
                var existingSetting = await _context.CoursesSettings
                    .FirstOrDefaultAsync(cs => cs.Id == dto.Course.SettingId && cs.CreatedBy == userId);

                if (existingSetting == null)
                {
                    return new ResponseDto<CourseWithSettingDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = MessagesConstant.CRS_INVALID_SETTING
                    };
                }

                // Duplicar la configuración existente
                duplicatedSettingEntity = new CourseSettingEntity
                {
                    Name = existingSetting.Name,
                    ScoreType = existingSetting.ScoreType,
                    StartDate = existingSetting.StartDate,
                    EndDate = existingSetting.EndDate,
                    MinimumGrade = existingSetting.MinimumGrade,
                    MaximumGrade = existingSetting.MaximumGrade,
                    MinimumAttendanceTime = existingSetting.MinimumAttendanceTime,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsOriginal = false // Marcamos como copia
                };
            }
            else // Caso 2: Crear una nueva configuración original
            {
                // Crear la configuración original
                var originalSettingEntity = new CourseSettingEntity
                {
                    Name = dto.CourseSetting.Name,
                    ScoreType = dto.CourseSetting.ScoreType,
                    StartDate = dto.CourseSetting.StartDate,
                    EndDate = dto.CourseSetting.EndDate,
                    MinimumGrade = dto.CourseSetting.MinimumGrade,
                    MaximumGrade = dto.CourseSetting.MaximumGrade,
                    MinimumAttendanceTime = dto.CourseSetting.MinimumAttendanceTime,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsOriginal = true // Marcamos como configuración original
                };

                // Guardar la configuración original en la base de datos
                _context.CoursesSettings.Add(originalSettingEntity);
                await _context.SaveChangesAsync();

                // Duplicar la configuración antes de asignarla al curso
                duplicatedSettingEntity = new CourseSettingEntity
                {
                    Name = originalSettingEntity.Name,
                    ScoreType = originalSettingEntity.ScoreType,
                    StartDate = originalSettingEntity.StartDate,
                    EndDate = originalSettingEntity.EndDate,
                    MinimumGrade = originalSettingEntity.MinimumGrade,
                    MaximumGrade = originalSettingEntity.MaximumGrade,
                    MinimumAttendanceTime = originalSettingEntity.MinimumAttendanceTime,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsOriginal = false // La copia siempre es marcada como no original
                };
            }

            // Guardar la copia en la base de datos
            _context.CoursesSettings.Add(duplicatedSettingEntity);
            await _context.SaveChangesAsync();

            // Crear el curso y asociarlo con la configuración duplicada
            var courseEntity = new CourseEntity
            {
                Name = dto.Course.Name,
                Section = dto.Course.Section,
                StartTime = dto.Course.StartTime,
                FinishTime = dto.Course.FinishTime,
                Code = dto.Course.Code,
                IsActive = true, // Por defecto el curso se deja activo
                CenterId = dto.Course.CenterId,
                CreatedBy = userId,
                UpdatedBy = userId,
                CourseSetting = duplicatedSettingEntity, // Asociamos la configuración duplicada
                SettingId = duplicatedSettingEntity.Id // Asignamos el ID de la configuración duplicada
            };

            // Guardar el curso en la base de datos
            _context.Courses.Add(courseEntity);
            await _context.SaveChangesAsync();

            // Mapear a DTO para la respuesta
            var courseDto = _mapper.Map<CourseWithSettingDto>(courseEntity);
            return new ResponseDto<CourseWithSettingDto>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.CRS_CREATE_SUCCESS,
                Data = courseDto
            };
        }

        // CP -> Editar un curso 
        public async Task<ResponseDto<CourseDto>> EditAsync(CourseEditDto dto, Guid id)
        {
            var userId = _auditService.GetUserId();

            // Incluir la relación con settings
            var courseEntity = await _context.Courses
                .Include(c => c.CourseSetting)
                .FirstOrDefaultAsync(x => x.Id == id && x.CreatedBy == userId); // Solo el creador puede modificarlo

            if (courseEntity == null)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };
            }

            _mapper.Map(dto, courseEntity);

            _context.Courses.Update(courseEntity);
            await _context.SaveChangesAsync();

            var courseDto = _mapper.Map<CourseDto>(courseEntity);

            return new ResponseDto<CourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CRS_UPDATE_SUCCESS,
                Data = courseDto
            };
        }


        // CP -> Eliminar un curso
        public async Task<ResponseDto<CourseWithSettingDto>> DeleteAsync(Guid id)
        {
            var userId = _auditService.GetUserId();

            var courseEntity = await _context.Courses
                .Include(c => c.CourseSetting) // Incluir la configuración asociada
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == userId); // Solo quien crea la clase puede borrarla

            if (courseEntity == null)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };
            }

            // Eliminar registros relacionados en course_notes
            var courseNotes = await _context.CoursesNotes
                .Where(cn => cn.CourseId == id)
                .ToListAsync();
            _context.CoursesNotes.RemoveRange(courseNotes);

            // Eliminar registros relacionados en students_activities_notes
            var units = await _context.Units
                .Where(u => u.CourseId == id)
                .ToListAsync();

            foreach (var unit in units)
            {
                var activities = await _context.Activities
                    .Where(a => a.UnitId == unit.Id)
                    .ToListAsync();

                foreach (var activity in activities)
                {
                    var notes = await _context.StudentsActivitiesNotes
                        .Where(n => n.ActivityId == activity.Id)
                        .ToListAsync();
                    _context.StudentsActivitiesNotes.RemoveRange(notes);
                }

                _context.Activities.RemoveRange(activities);
            }

            // Eliminar las unidades relacionadas
            _context.Units.RemoveRange(units);

            // Eliminar registros relacionados en attendances
            var attendances = await _context.Attendances
                .Where(a => a.CourseId == id)
                .ToListAsync();
            _context.Attendances.RemoveRange(attendances);

            // Eliminar registros relacionados en students_courses
            var studentCourses = await _context.StudentsCourses
                .Where(sc => sc.CourseId == id)
                .ToListAsync();
            _context.StudentsCourses.RemoveRange(studentCourses);

            // Eliminar la configuración asociada al curso
            if (courseEntity.CourseSetting != null && !courseEntity.CourseSetting.IsOriginal)
            {
                _context.CoursesSettings.Remove(courseEntity.CourseSetting);
            }

            // Finalmente, eliminar el curso
            _context.Courses.Remove(courseEntity);
            await _context.SaveChangesAsync();

            return new ResponseDto<CourseWithSettingDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CRS_DELETE_SUCCESS
            };
        }
    }
}
