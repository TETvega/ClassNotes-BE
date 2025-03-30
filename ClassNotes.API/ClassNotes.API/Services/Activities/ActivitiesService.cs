using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace ClassNotes.API.Services.Activities
{
    // --------------------- CP --------------------- //
    public class ActivitiesService : IActivitiesService
    {
        private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly int PAGE_SIZE;

        public ActivitiesService(
            ClassNotesContext context,
            IMapper mapper,
            IConfiguration configuration,
            IAuditService auditService
        )
        {
            _context = context;
            _mapper = mapper;
             // Ahora la paginación se maneja de la siguiente forma:
             // PageSize ahora es un objeto que contiene un valor especifico para los diferentes servicios
             // Y este se usa de la manera que aparece abajo:
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Activities");
            _auditService = auditService;
        }


        // Traer todas las actividades (Paginadas)
        public async Task<ResponseDto<PaginationDto<List<ActivitySummaryDto>>>> GetActivitiesListAsync(
    string searchTerm = "",
    int page = 1)
        {
            var userId = _auditService.GetUserId(); // Id de quien hace la petición

            int startIndex = (page - 1) * PAGE_SIZE;
            var activitiesQuery = _context.Activities
                .Where(c => c.CreatedBy == userId) // Para mostrar unicamente los cursos que pertenecen al usuario que hace la petición
                .Include(a => a.Unit) // Cargar la relación Unit
                .ThenInclude(u => u.Course) // Cargar la relación Course desde Unit
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                activitiesQuery = activitiesQuery
                    .Where(
                        x => x.Name.ToLower().Contains(searchTerm.ToLower()) || // buscar por nombre de la actividad
                        x.Unit.Course.Name.ToLower().Contains(searchTerm.ToLower()) // Buscar por el nombre del curso
                    );
            }

            int totalActivities = await activitiesQuery.CountAsync(); // total de las actividades
            int totalPages = (int)Math.Ceiling((double)totalActivities / PAGE_SIZE); // total de las paginas

            var activitiesEntity = await activitiesQuery
                .OrderByDescending(x => x.CreatedDate) // se ordena por fecha de creación
                .Skip(startIndex) // Omite el indice
                .Take(PAGE_SIZE) // muestra la cantidad de items que se definio en el page size
                .ToListAsync();

            var activitiesDto = _mapper.Map<List<ActivitySummaryDto>>(activitiesEntity);

            return new ResponseDto<PaginationDto<List<ActivitySummaryDto>>> // Aqui ya se estuctura la respuesta a mostrar
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_RECORDS_FOUND,
                Data = new PaginationDto<List<ActivitySummaryDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalActivities,
                    TotalPages = totalPages,
                    Items = activitiesDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        public async Task<ResponseDto<List<StudentActivityNoteDto>>> ReviewActivityAsync(List<StudentActivityNoteCreateDto> dto, Guid ActivityId)
        {
            //(Ken)
            //Obtenemos la propia actividad especificada...
            var activityEntity = await _context.Activities.Include(a => a.Unit).FirstOrDefaultAsync(a => a.Id == ActivityId);
            
            //Transformacion de las reviciones proporcionadas en forma de StudentActivityNoteCreateDto a entities
            var studentActivityEntity = _mapper.Map<List<StudentActivityNoteEntity>>(dto);
            
            //Busqueda de las studentUnits de ese curso, para calcular promedios...
            var studentsUnits = _context.StudentsUnits.Include(a => a.StudentCourse).Where(x => x.StudentCourse.CourseId == activityEntity.Unit.CourseId);

            //Verificacion de existencia...
            if (activityEntity == null)
            {
                return new ResponseDto<List<StudentActivityNoteDto>>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }

            //Por cada actividad revisada enviada por el usuario...
            foreach (var activity in studentActivityEntity)
            {
                //Se le incorpora a la entidad su activity id
                activity.ActivityId = ActivityId;




                //Se filtran las studentUnits para tener solo las del estudiante asociado a la actividad revisada actual,
                //para questiones de promedio...
                var studentUnitEntity = studentsUnits.Include(a => a.StudentCourse).Where(a => a.StudentCourse.StudentId == activity.StudentId);




                //De las filtradas, se busca la de la misma unidad que esta actividad, para cambiar su nota...
                var individualStudentUnit = studentUnitEntity.Include(a => a.StudentCourse).FirstOrDefault(x => x.UnitId == activityEntity.UnitId);
                //Busqueda de ntidad de cstudentCourse para confirmar que s esta en la clase el estudiante
                var testStudentCourse = _context.StudentsCourses.FirstOrDefault(x => x.StudentId == activity.StudentId && x.CourseId == activityEntity.Unit.CourseId);
               
                if (testStudentCourse == null)
                {
                    return new ResponseDto<List<StudentActivityNoteDto>>
                    {
                        StatusCode = 404,
                        Status = false,
                        Message = MessagesConstant.RECORD_NOT_FOUND
                    };
                }


                //Si esta en la clase pero no tiene studentUnit, se crea un studentUnit...
                if (individualStudentUnit == null)
                {

                    var newStudentUnit = new StudentUnitEntity
                    {
                        UnitNote = 0,
                        UnitNumber = activityEntity.Unit.UnitNumber,
                        UnitId = activityEntity.UnitId,
                        StudentCourseId = testStudentCourse.Id
                    };
                    _context.StudentsUnits.Add(newStudentUnit);
                    await _context.SaveChangesAsync();

                    individualStudentUnit = newStudentUnit;
                }

                //Se verifica que la calificacion sea valida...
                if (activity.Note > activityEntity.MaxScore || activity.Note < 0)
                {
                    return new ResponseDto<List<StudentActivityNoteDto>>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "Se ingresó una calificación no valida."
                    };
                }

                //Se buscan las otras actividades en la unidad del estudiate
                var studentActivities =   _context.StudentsActivitiesNotes.Where(x => x.Activity.UnitId == activityEntity.UnitId && x.StudentId == activity.StudentId);

                //En esta lista se almacenaran los puntajes de estas actividades, para asi recalcular el promedio del parcial...
                var totalUnitPoints = new List<float>();

                //Si no hay otras actividades, la lista solo tendra la nota de esta actividad...
                if (studentActivities.Count() == 0)
                {
                    totalUnitPoints.Add(activity.Note);
                }
                else
                {

                    foreach (var revisedActivity in studentActivities)
                    {
                        totalUnitPoints.Add(revisedActivity.Note);
                    }
                    totalUnitPoints.Add(activity.Note);

                }


                //Calculo del promedio de la unidad para almacenar en la entidad
                var newUnitScore = totalUnitPoints.Sum()/totalUnitPoints.Count();
                individualStudentUnit.UnitNote = newUnitScore;

                _context.StudentsUnits.Update(individualStudentUnit);
                await _context.SaveChangesAsync();

                //Procedimiento similar pero para el curso del estudiante...

                //En esa lista se almacenan los puntajes de unidad del estudiante...
                var totalPoints = new List<float>();
                foreach (var unit in studentUnitEntity)
                {
                    totalPoints.Add(unit.UnitNote);
                }

                //Se actualiza la nota en el curso del estudiante usando los valores almacenados...
                individualStudentUnit.StudentCourse.FinalNote = totalPoints.Sum() / totalPoints.Count();


                _context.StudentsCourses.Update(individualStudentUnit.StudentCourse);
                await _context.SaveChangesAsync(); 

                //Se busca la relacion actividad a estudiante para confirmar su existencia...
                var existingStudentActivity = _context.StudentsActivitiesNotes.FirstOrDefault(x => x.ActivityId == ActivityId && x.StudentId == activity.StudentId);
               

                //Si existe, se actualizan sus datos, sino, se crea...
                if (existingStudentActivity != null)
                {
                    existingStudentActivity.Note = activity.Note;
                    existingStudentActivity.Feedback = activity.Feedback;

                    _context.StudentsActivitiesNotes.Update(existingStudentActivity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.StudentsActivitiesNotes.Add(activity);
                    await _context.SaveChangesAsync();
                }

            };

            var studentActivityDto = _mapper.Map<List<StudentActivityNoteDto>>(studentActivityEntity);
            return new ResponseDto<List<StudentActivityNoteDto>>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.CREATE_SUCCESS,
                Data = studentActivityDto
            };

        }

        // Obtener una actividad mediante su id
        public async Task<ResponseDto<ActivityDto>> GetActivityByIdAsync(Guid id)
        {
            var userId = _auditService.GetUserId(); // id de quien hace la petición

            var activityEntity = await _context.Activities
                .Where(a => a.CreatedBy == userId) // Para que solo aparezca si lo creo quien hace la petición
                .Include(a => a.Unit) // Incluir las unidades
                .ThenInclude(u => u.Course) // Incluir los cursos
                .FirstOrDefaultAsync(a => a.Id == id); 

            if (activityEntity == null) // Si no existe la actividad
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ACT_RECORD_NOT_FOUND
                };
            }

            var activityDto = _mapper.Map<ActivityDto>(activityEntity);
            return new ResponseDto<ActivityDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_RECORDS_FOUND,
                Data = activityDto
            };
        }

        // Crear una actividad
        public async Task<ResponseDto<ActivityDto>> CreateAsync(ActivityCreateDto dto)
        {
            // Validar que la fecha de calificación no sea menor a la fecha actual
            if (dto.QualificationDate < DateTime.UtcNow.Date)
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.ACT_QUALIFICATION_DATE_INVALID
                };
            }

            var activityEntity = _mapper.Map<ActivityEntity>(dto);
            _context.Activities.Add(activityEntity);
            await _context.SaveChangesAsync();
            var activityDto = _mapper.Map<ActivityDto>(activityEntity);
            return new ResponseDto<ActivityDto>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.ACT_CREATE_SUCCESS,
                Data = activityDto
            };
        }

        // Editar una actividad
        public async Task<ResponseDto<ActivityDto>> EditAsync(ActivityEditDto dto, Guid id)
        {
            var userId = _auditService.GetUserId();

            var activityEntity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == userId);

            // Validar que la fecha de calificación no sea menor a la fecha actual
            if (dto.QualificationDate < DateTime.UtcNow.Date)
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.ACT_QUALIFICATION_DATE_INVALID
                };
            }

            if (activityEntity == null)
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ACT_RECORD_NOT_FOUND
                };
            }

            _mapper.Map(dto, activityEntity);
            _context.Activities.Update(activityEntity);
            await _context.SaveChangesAsync();
            var activityDto = _mapper.Map<ActivityDto>(activityEntity);
            return new ResponseDto<ActivityDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_UPDATE_SUCCESS,
                Data = activityDto
            };
        }

        // Eliminar una actividad
        public async Task<ResponseDto<ActivityDto>> DeleteAsync(Guid id)
        {
            var userId = _auditService.GetUserId();
            var activityEntity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == userId);
            if (activityEntity == null)
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ACT_RECORD_NOT_FOUND
                };
            }
            _context.Activities.Remove(activityEntity);
            await _context.SaveChangesAsync();
            return new ResponseDto<ActivityDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_DELETE_SUCCESS
            };
        }
    }
}
