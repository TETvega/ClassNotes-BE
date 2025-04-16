using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.CourseSettings;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using NetTopologySuite.Geometries;
using NetTopologySuite;

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
            int currentPageSize =  Math.Max(1, pageSize ?? PAGE_SIZE);
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

        //(Ken)
        //Obtener unidades...
        public async Task<ResponseDto<List<UnitDto>>> GetCourseUnits(Guid id)
        {
            var userId = _auditService.GetUserId(); //Obtiene id de usuario...


            var unitEntities = await _context.Units 
                .Where(x => x.CourseId == id && x.CreatedBy == userId)//Busca todas las unidades del curso especificado por el id y creadas por el usuario...
                .OrderBy(n => n.UnitNumber)//Ordena según numero de unidad...
                .ToListAsync();

            var unitDtos = _mapper.Map<List<UnitDto>>(unitEntities);

            return new ResponseDto<List<UnitDto>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CRS_RECORD_FOUND,
                Data = unitDtos
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

            //(Ken) creación de unidades..

            //Valida que se cree correctamente el tipo de puntaje... esos son los valores que se pueden usar...
            if (dto.CourseSetting.ScoreType.ToUpper().Trim() != ScoreTypeConstant.GOLD_SCORE &&
                dto.CourseSetting.ScoreType.ToUpper().Trim() != ScoreTypeConstant.WEIGHTED_SCORE &&
                dto.CourseSetting.ScoreType.ToUpper().Trim() != ScoreTypeConstant.ARITHMETIC_SCORE)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 405,
                    Status = false,
                    Message = $"Los tipos de puntaje válidos son: [ {ScoreTypeConstant.GOLD_SCORE} , {ScoreTypeConstant.WEIGHTED_SCORE} , {ScoreTypeConstant.ARITHMETIC_SCORE}  ]"
                };
            }


            //Lista de dtos de  unidades...
            var UnitList = dto.Units;

            //Evalua que no se reciban null si no es oro...
            if(UnitList.Select(x => x.MaxScore).ToList().Contains(null) && dto.CourseSetting.ScoreType.ToUpper().Trim() != ScoreTypeConstant.GOLD_SCORE)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 405,
                    Status = false,
                    Message = "El valor máximo de unidad no debe ir vacío a menos que evalue puntos oro."
                };
            }

            //Si es ponderado, la suma de Maxscore de todas las unidades debe ser igual al máximo de el curso...
            if (UnitList.Select(x => x.MaxScore).ToList().Sum() != dto.CourseSetting.MaximumGrade && dto.CourseSetting.ScoreType.ToUpper().Trim() == ScoreTypeConstant.WEIGHTED_SCORE)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 405,
                    Status = false,
                    Message = "Se ingreso valores de Unidad no válidos"
                };
            }

            //En esta lista de guardaran los numeros de unidad para ir verificando que no se repitan...
            List<int> unitNumbers = [];

            //Esta lista es para hacer addRange de las nuevas entidades de unidad...
            List<UnitEntity> newUnitEntityList = [];
            foreach (var unit in UnitList)
            {
                //No se puede ingresar valores maximos  de unidad iguales o menores a 0, al menos si no es oro.
                if (unit.MaxScore <= 0 && dto.CourseSetting.ScoreType.ToUpper().Trim() != ScoreTypeConstant.GOLD_SCORE)
                {
                    return new ResponseDto<CourseWithSettingDto>
                    {
                        StatusCode = 405,
                        Status = false,
                        Message = "Se ingreso un valor de Unidad no válido"
                    };
                }

                //Con este if se verifica que no sean repetidos los números de unidad...
                if (unitNumbers.Contains(unit.UnitNumber))
                {
                    return new ResponseDto<CourseWithSettingDto>
                    {
                        StatusCode = 405,
                        Status = false,
                        Message = "No se puede repetir el número de unidad"
                    };
                }

                //Si no lo son, se incluye en la lista para ser verificado en la siguiente iteración
                unitNumbers.Add(unit.UnitNumber);


                float? unitMax = 0;

                //Si es aritmetico, se guarda la nota máxima de la unidad como la divición entre el puntaje maximo del curso
                // y la cantidad de unidades, para asegurar que sean iguales...
                if (dto.CourseSetting.ScoreType.ToUpper().Trim() == ScoreTypeConstant.ARITHMETIC_SCORE)
                {
                    unitMax = dto.CourseSetting.MaximumGrade/UnitList.Count();
                }
                //Si es oro, forzamos null vaya lo que vaya
                else if(dto.CourseSetting.ScoreType.ToUpper().Trim() == ScoreTypeConstant.GOLD_SCORE)
                {
                    unitMax = null;
                }

                // si no, es aritmetico, se almacena directamente...
                else
                {
                    unitMax = unit.MaxScore;
                }

                //Mapeo manual para la unidad nueva...
                var newUnitEntity = new UnitEntity
                {
                    UnitNumber = unit.UnitNumber,
            
                    MaxScore = unitMax,
                };

                //Se ingresa a la lista a la que se le hará addRange
                newUnitEntityList.Add(newUnitEntity);
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
                    ScoreType = existingSetting.ScoreType.ToUpper().Trim(),
                    StartDate = existingSetting.StartDate,
                    EndDate = existingSetting.EndDate,
                    MinimumGrade = existingSetting.MinimumGrade,
                    MaximumGrade = existingSetting.MaximumGrade,
                    MinimumAttendanceTime = existingSetting.MinimumAttendanceTime,
                    GeoLocation = existingSetting.GeoLocation,
                    ValidateRangeMeters = existingSetting.ValidateRangeMeters,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsOriginal = false // Marcamos como copia
                };
            }
            else // Caso 2: Crear una nueva configuración original
            {
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                var point = geometryFactory.CreatePoint(new Coordinate(dto.CourseSetting.GetLocationDto.X, dto.CourseSetting.GetLocationDto.Y));
                // Crear la configuración original
                var originalSettingEntity = new CourseSettingEntity
                {
                    Name = dto.CourseSetting.Name,
                    ScoreType = dto.CourseSetting.ScoreType.ToUpper().Trim(),
                    StartDate = dto.CourseSetting.StartDate,
                    EndDate = dto.CourseSetting.EndDate,
                    MinimumGrade = dto.CourseSetting.MinimumGrade,
                    MaximumGrade = dto.CourseSetting.MaximumGrade,
                    ValidateRangeMeters = dto.CourseSetting.ValidateRangeMeters,
                    MinimumAttendanceTime = dto.CourseSetting.MinimumAttendanceTime,
                    GeoLocation = point,
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
                    ScoreType = originalSettingEntity.ScoreType.ToUpper().Trim(),
                    StartDate = originalSettingEntity.StartDate,
                    EndDate = originalSettingEntity.EndDate,
                    MinimumGrade = originalSettingEntity.MinimumGrade,
                    MaximumGrade = originalSettingEntity.MaximumGrade,
                    MinimumAttendanceTime = originalSettingEntity.MinimumAttendanceTime,
                    GeoLocation = point,
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

            //(ken) para que cada entidad tenga su courseEntity
            foreach (var unitEntity in newUnitEntityList)
            {
                unitEntity.CourseId = courseEntity.Id;
            }

            _context.Units.AddRange(newUnitEntityList);
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
        // TODO





        // Podra editar la configuracion de GEOLOCALIZACION 
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

        public async Task<ResponseDto<CourseWithSettingDto>> EditUbicationAsync(LocationDto dto, Guid id)
        {
            var userId = _auditService.GetUserId();

            // Validar si no se mandó lat/lng
            if (dto.X == 0 || dto.Y == 0)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Ubicación inválida"
                };
            }

            // Obtener el curso con su CourseSetting
            var course = await _context.Courses
                .Include(c => c.CourseSetting)
                .FirstOrDefaultAsync(c => c.Id == id  && c.CreatedBy == userId);

            if (course == null || course.CourseSetting == null)
            {
                return new ResponseDto<CourseWithSettingDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = "Curso o configuración no encontrados"
                };
            }

            // Crear el punto de ubicación
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var point = geometryFactory.CreatePoint(new Coordinate(dto.X, dto.Y));

            // Actualizar la geolocalización
            course.CourseSetting.GeoLocation = point;

            // Guardar cambios
            await _context.SaveChangesAsync();

            var courseDto = _mapper.Map<CourseWithSettingDto>(course);

            return new ResponseDto<CourseWithSettingDto>
            {
                StatusCode = 200,
                Status = true,
                Message = "Ubicación actualizada correctamente",
                Data = courseDto
            };
        }


    }
}
