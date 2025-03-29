using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Services.Audit;
using iText.Kernel.Geom;
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

        public async Task<ResponseDto<PaginationDto<List<CourseDto>>>> GetCoursesListAsync(
            string searchTerm = "",
            int page = 1,
            int? pageSize = null
            )
        {
            /** HR
             * Si pageSize es -1, se devuelve int.MaxValue
             * -1 significa "obtener todos los elementos", por lo que usamos int.MaxValue 
             *  int.MaxValue es 2,147,483,647, que es el valor máximo que puede tener un int en C#.
             *  Math.Max(1, valor) garantiza que currentPageSize nunca sea menor que 1 excepto el -1 al inicio
             *  si pageSize es nulo toma el valor de PAGE_SIZE
             */
            int currentPageSize = pageSize == -1 ? int.MaxValue : Math.Max(1, pageSize ?? PAGE_SIZE);
            int startIndex = ( page   - 1) * currentPageSize;

            var userId = _auditService.GetUserId();

            // Query base con filtro por usuario
            var coursesQuery = _context.Courses
                .Include(c => c.CourseSetting)
                .Where(c => c.CreatedBy == userId);

            // Filtro por búsqueda es lo mismo aplicado a courses
            // HR
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string pattern = $"%{searchTerm}%";
                coursesQuery = coursesQuery.Where(c =>
                    EF.Functions.Like(c.Name, pattern) ||
                    EF.Functions.Like(c.Code, pattern));
            }

            int totalItems = await coursesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);

            // aplicar paginacion 

            var courseEntities = await coursesQuery
                .OrderByDescending(n => n.Section) //Ordenara por seccion   
                .Skip(startIndex)
                .Take(currentPageSize)
                .ToListAsync();

            var coursesDto = _mapper.Map<List<CourseDto>>(courseEntities);

            return new ResponseDto<PaginationDto<List<CourseDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_RECORDS_FOUND,
                Data = new PaginationDto<List<CourseDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = coursesDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }


        // CP -> Para listar un curso mediante su id
        public async Task<ResponseDto<CourseDto>> GetCourseByIdAsync(Guid id)
        {
            var userId = _auditService.GetUserId();

            var courseEntity = await _context.Courses
                .Include(c => c.CourseSetting) // Para incluir la información de la configuración del curso
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == userId); // Unicamente aprecera el curso si lo creo quien hace la petición
            if (courseEntity == null)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };
            }
            var courseDto = _mapper.Map<CourseDto>(courseEntity);
            return new ResponseDto<CourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_RECORDS_FOUND,
                Data = courseDto
            };
        }

        // CP -> Crear un curso
        public async Task<ResponseDto<CourseDto>> CreateAsync(CourseCreateDto dto)
        {
            var userId = _auditService.GetUserId();

            // Validar que la hora de fin no sea menor a la de inicio
            if (dto.FinishTime <= dto.StartTime)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CNS_END_TIME_BEFORE_START_TIME
                };
            }

            // Validar que la fecha de fin no sea menor a la de inicio
            if (dto.EndDate <= dto.StartDate)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CP_INVALID_DATES
                };
            }

            // Validar que las notas sean válidas
            if (dto.MinimumGrade <= 0 || dto.MaximumGrade <= 0 || dto.MaximumGrade < dto.MinimumGrade)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CP_INVALID_GRADES
                };
            }

            // Verificar si ya existe una clase con el mismo nombre, sección, codigo y hora de inicio
            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.CreatedBy == userId &&
                    c.Name.ToLower() == dto.Name.ToLower() &&
                    c.Section.ToLower() == dto.Section.ToLower() &&
                    c.Code.ToLower() == dto.Code.ToLower() &&
                    c.StartTime == dto.StartTime
                );
            if (existingCourse != null)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.CNS_CLASS_ALREADY_EXISTS
                };
            }


            // Crear o duplicar la configuración del curso
            CourseSettingEntity originalSettingEntity;
            CourseSettingEntity duplicatedSettingEntity;

            if (dto.SettingId.HasValue && dto.SettingId != Guid.Empty) // Caso 1: Duplicar una configuración existente
            {
                var existingSetting = await _context.CoursesSettings
                    .FirstOrDefaultAsync(cs => cs.Id == dto.SettingId && cs.CreatedBy == userId);

                if (existingSetting == null)
                {
                    return new ResponseDto<CourseDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = MessagesConstant.CRS_INVALID_SETTING
                    };
                }

                // La configuración original es la existente
                originalSettingEntity = existingSetting;

                // Duplicar el course_setting
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
            else // Caso 2: Crear una configuración original
            {
                // Validar que el nombre de la configuración esté presente
                if (string.IsNullOrEmpty(dto.SettingName))
                {
                    return new ResponseDto<CourseDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = MessagesConstant.CP_SETTING_NAME_REQUIRED
                    };
                }

                // Crear la configuración original
                originalSettingEntity = new CourseSettingEntity
                {
                    Name = dto.SettingName,
                    ScoreType = dto.ScoreType,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    MinimumGrade = dto.MinimumGrade,
                    MaximumGrade = dto.MaximumGrade,
                    MinimumAttendanceTime = dto.MinimumAttendanceTime,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsOriginal = true // Marcamos como configuración original
                };

                // Guardar la configuración original en la base de datos
                _context.CoursesSettings.Add(originalSettingEntity);
                await _context.SaveChangesAsync();

                // Siempre duplicar la configuración antes de asignarla al curso
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

            // Crear el curso y asociarlo con la configuración
            var courseEntity = new CourseEntity
            {
                Name = dto.Name,
                Section = dto.Section,
                StartTime = dto.StartTime,
                FinishTime = dto.FinishTime,
                Code = dto.Code,
                IsActive = dto.IsActive,
                CenterId = dto.CenterId,
                CreatedBy = userId,
                UpdatedBy = userId,
                CourseSetting = duplicatedSettingEntity, // Asociamos la configuración
                SettingId = duplicatedSettingEntity.Id // Asignamos el ID de la configuración
            };

            // Guardar el curso en la base de datos
            _context.Courses.Add(courseEntity);
            await _context.SaveChangesAsync();

            // Mapear a DTO para la respuesta
            var courseDto = _mapper.Map<CourseDto>(courseEntity);

            return new ResponseDto<CourseDto>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.CNS_CREATE_SUCCESS,
                Data = courseDto
            };
        }

        // EG -> Editar un curso 

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

            // Verificar si la configuración del curso también debe actualizarse
            if (courseEntity.CourseSetting != null && dto.SettingId != Guid.Empty)
            {
                // Actualizar solo si es necesario
                courseEntity.SettingId = dto.SettingId;

            }

            _context.Courses.Update(courseEntity);
            await _context.SaveChangesAsync();

            var courseDto = _mapper.Map<CourseDto>(courseEntity);

            return new ResponseDto<CourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_UPDATE_SUCCESS,
                Data = courseDto
            };
        }


        // CP -> Eliminar un curso
        public async Task<ResponseDto<CourseDto>> DeleteAsync(Guid id)
        {
            var userId = _auditService.GetUserId();

            var courseEntity = await _context.Courses
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == userId); // Solo quien crea la clase puede borrarla

            if (courseEntity == null)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };
            }

            // Elimina los registros relacionados en course_notes
            var courseNotes = await _context.CoursesNotes
                .Where(cn => cn.CourseId == id)
                .ToListAsync();

            _context.CoursesNotes.RemoveRange(courseNotes);

            // Elimina los registros relacionados en students_activities_notes
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

            // Elimina las unidades relacionadas
            _context.Units.RemoveRange(units);

            // Elimina los registros relacionados en attendances
            var attendances = await _context.Attendances
                .Where(a => a.CourseId == id)
                .ToListAsync();

            _context.Attendances.RemoveRange(attendances);

            // Elimina los registros relacionados en students_courses
            var studentCourses = await _context.StudentsCourses
                .Where(sc => sc.CourseId == id)
                .ToListAsync();

            _context.StudentsCourses.RemoveRange(studentCourses);

            _context.Courses.Remove(courseEntity);

            await _context.SaveChangesAsync();

            return new ResponseDto<CourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_DELETE_SUCCESS
            };
        }
    }
}
