using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Audit;
using ClassNotes.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Channels;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace ClassNotes.API.Services.Activities
{
    // --------------------- CP --------------------- //
    public class ActivitiesService : IActivitiesService
    {
        private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly Channel<EmailFeedBackRequest> _emailQueue;
        private readonly int PAGE_SIZE;

        public ActivitiesService(
            ClassNotesContext context,
            IMapper mapper,
            IConfiguration configuration,
            IAuditService auditService,
            Channel<EmailFeedBackRequest> emailQueue
        )
        {
            _context = context;
            _mapper = mapper;
            // Ahora la paginación se maneja de la siguiente forma:
            // PageSize ahora es un objeto que contiene un valor especifico para los diferentes servicios
            // Y este se usa de la manera que aparece abajo:
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Activities");
            _auditService = auditService;
            _emailQueue = emailQueue;
        }


        // Traer todas las actividades (Paginadas)
        public async Task<ResponseDto<PaginationDto<List<ActivitySummaryDto>>>> GetActivitiesListAsync(
            string searchTerm = "",
            int page = 1,
            int? pageSize = 10,
            Guid? centerId = null,
            Guid? tagActivityId = null,
            string typeActivities = "ALL"
        )
        {
            var allowedValues = new HashSet<string> { "ALL", "PENDING", "DONE" };

            if (!allowedValues.Contains(typeActivities.ToUpper()))
            {
                return new ResponseDto<PaginationDto<List<ActivitySummaryDto>>>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "typeActivities solo puede ser  [ ALL , PENDING , DONE ]",
                    Data = null,
                };
            }
            var userId = _auditService.GetUserId(); // Id de quien hace la petición

            int MAX_PAGE_SIZE = 50;
            int currentPageSize = Math.Min(pageSize ?? PAGE_SIZE, MAX_PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;


            var now = DateTime.UtcNow;

            var activitiesQuery = _context.Activities
                .AsNoTracking()
                .Where(a => a.CreatedBy == userId)
                .Select(a => new
                {
                    Activity = a,
                    Course = a.Unit.Course,
                    Center = a.Unit.Course.Center,
                    TagActivity = a.TagActivity,
                    ActiveStudentIds = _context.StudentsCourses
                        .Where(sc => sc.CourseId == a.Unit.CourseId && sc.IsActive)
                        .Select(sc => sc.StudentId)
                        .ToList(),
                    StudentNoteIds = a.StudentNotes.Select(sn => sn.StudentId).ToList()
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                activitiesQuery = activitiesQuery
                    .Where(x => x.Activity.Name.ToLower().Contains(searchTerm) ||
                                x.Course.Name.ToLower().Contains(searchTerm));
            }

            if (centerId.HasValue)
            {
                activitiesQuery = activitiesQuery
                    .Where(x => x.Course.CenterId == centerId.Value);
            }

            if (tagActivityId.HasValue)
            {
                activitiesQuery = activitiesQuery
                    .Where(x => x.Activity.TagActivityId == tagActivityId.Value);
            }

            if (typeActivities?.ToUpper() == "PENDING")
            {
                // Actividad pendiente si falta algún estudiante sin nota
                activitiesQuery = activitiesQuery
                    .Where(x => x.Activity.QualificationDate < now &&
                                x.ActiveStudentIds.Except(x.StudentNoteIds).Any());
            }
            if (typeActivities?.ToUpper() == "DONE")
            {
                // Actividad realizada si todos los estudiantes tienen notas
                activitiesQuery = activitiesQuery
                    .Where(x => x.Activity.QualificationDate < now &&
                                x.ActiveStudentIds.All(studentId => x.StudentNoteIds.Contains(studentId)));
            }

            // Total y paginado
            var totalActivities = await activitiesQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalActivities / currentPageSize);

            // Paginación y ordenación
            var activitiesEntity = await activitiesQuery
                .OrderByDescending(x => x.Activity.CreatedDate) // Ordenar por fecha de creación
                .Skip(startIndex)
                .Take(currentPageSize)
                .ToListAsync();

            // Mapear a DTO
            // centro retorna nulo siempre revisar 
            // depurar cada campo ->
            var activitiesDto = activitiesEntity.Select(x => new ActivitySummaryDto
            {
                Id = x.Activity?.Id ?? Guid.Empty,
                Name = x.Activity?.Name ?? "Sin nombre",
                Description = x.Activity?.Description ?? "Sin descripción",
                QualificationDate = x.Activity?.QualificationDate ?? DateTime.MinValue,
                TagActivityId = x.Activity?.TagActivityId ?? Guid.Empty,
                CourseId = x.Course?.Id ?? Guid.Empty,
                CourseName = x.Course?.Name ?? "Sin nombre de curso",
                CenterId = x.Course?.CenterId ?? Guid.Empty,
                CenterName = x.Center?.Name ?? "Sin nombre de centro", // nulo por algun arazon
                CenterAbb = x.Center?.Abbreviation ?? "Sin ABB"
            }).ToList();

            return new ResponseDto<PaginationDto<List<ActivitySummaryDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_RECORDS_FOUND,
                Data = new PaginationDto<List<ActivitySummaryDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
                    TotalItems = totalActivities,
                    TotalPages = totalPages,
                    Items = activitiesDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        //Mostrar listado de estudiantes junto a su nota en una actividad...            
        public async Task<ResponseDto<PaginationDto<List<StudentAndNoteDto>>>> GetStudentsActivityScoreAsync(Guid activityId, int page = 1, string searchTerm = "", int? pageSize = null)
        {

            int currentPageSize = Math.Max(1, pageSize ?? PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;



            var userId = _auditService.GetUserId();

            //Obtenemos la propia actividad especificada...
            var activityEntity = await _context.Activities.Include(a => a.Unit).FirstOrDefaultAsync(a => a.Id == activityId);

            //Verificacion de existencia...
            if (activityEntity == null)
            {
                return new ResponseDto<PaginationDto<List<StudentAndNoteDto>>>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }

            //Trae de la base de datos todos los esstudiantes en el curso del que sale la actividad especifica, creados por el usuario...
            //Incluye a student para poder poblar el listado de dtos con info del estudiante y activities de student para verificar la nota...
            var studentQuery = _context.StudentsCourses
                .Include(x => x.Student)
                .ThenInclude(y => y.Activities)
                    .AsQueryable()
                        .Where(c => c.CourseId == activityEntity.Unit.CourseId && c.CreatedBy == userId && c.IsActive);//Solo estudiantes activos...

            // Filtro por término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string pattern = $"%{searchTerm}%";
                studentQuery = studentQuery.Where(c =>
                    EF.Functions.Like((c.Student.FirstName/* + " "+c.Student.LastName*/), pattern) ||
                    EF.Functions.Like(c.Student.Email, pattern));
            }


            int totalItems = await studentQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);

            var studentNotesEntities = await studentQuery.ToListAsync();

            //Aqui se almacenaran los dtos a devolver...
            List<StudentAndNoteDto> studentScoreList = [];

            //Por cada actividad revisada obtenida...
            studentNotesEntities.ForEach(x =>
            {
                //Si existe una revision de actividad, se asigna ese valor, sino, se le pone como 0...
                var note = x.Student.Activities.FirstOrDefault(u => u.ActivityId == activityId)?.Note ?? 0;
                var feedBack = x.Student.Activities.FirstOrDefault(u => u.ActivityId == activityId)?.Feedback ;

                //Crea el dto y lo ingresa en la lista creada anteriormente...
                var studentAndNoteDto = new StudentAndNoteDto
                {
                    //Se utiliza la informacion de x.Student, por eso se incluyo en la llamada original...
                    Id = x.StudentId, 
                    Name = x.Student.FirstName + " " + x.Student.LastName,
                    Email = x.Student.Email,
                    Score = (note/100)*activityEntity.MaxScore,
                    FeedBack = feedBack
                };

                studentScoreList.Add(studentAndNoteDto);
            });


            studentScoreList = studentScoreList.OrderBy(n => (n.Name))
            .Skip(startIndex)
            .Take(currentPageSize).ToList();

            return new ResponseDto<PaginationDto<List<StudentAndNoteDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<StudentAndNoteDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = studentScoreList,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }


        public async Task<ResponseDto<List<StudentActivityNoteDto>>> ReviewActivityAsync(List<StudentActivityNoteCreateDto> dto, Guid ActivityId)
        {
            var userId = _auditService.GetUserId();
            //(Ken)
            //Obtenemos la propia actividad especificada...
            var activityEntity = await _context.Activities.Include(a => a.CreatedByUser).Include(a => a.Unit).ThenInclude(x => x.Course).FirstOrDefaultAsync(a => a.Id == ActivityId && a.CreatedBy == userId);


            //Para el servicio de enviar emails...
            var emailRequest = new EmailFeedBackRequest
            {
                TeacherEntity = activityEntity.CreatedByUser,
                ActivityEntity = activityEntity,
                Students = []
            };

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

            //busca el settings de ese curso, para obtener su configuración de nota...
            var courseSetting = await _context.CoursesSettings.FirstOrDefaultAsync(x => x.Id == activityEntity.Unit.Course.SettingId);

            //Transformacion de las reviciones proporcionadas en forma de StudentActivityNoteCreateDto a entities
            var studentActivityEntity = _mapper.Map<List<StudentActivityNoteEntity>>(dto);

            //Busqueda de las studentUnits de ese curso, para calcular promedios...
            var studentsUnits = _context.StudentsUnits.Include(a => a.Unit).Include(a => a.StudentCourse).Where(x => x.StudentCourse.CourseId == activityEntity.Unit.CourseId);


            //Por cada actividad revisada enviada por el usuario...
            foreach (var activity in studentActivityEntity)
            {
                //Se le incorpora a la entidad su activity id
                activity.ActivityId = ActivityId;

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

                //Se guarda la calificación como un promedio
                activity.Note = (activity.Note / activityEntity.MaxScore) * 100;

                //Se filtran las studentUnits para tener solo las del estudiante asociado a la actividad revisada actual,
                //para questiones de promedio...
                var studentUnitEntity = studentsUnits.Include(a => a.StudentCourse).Where(a => a.StudentCourse.StudentId == activity.StudentId);


                //De las filtradas, se busca la de la misma unidad que esta actividad, para cambiar su nota...
                var individualStudentUnit = studentUnitEntity.Include(a => a.Unit).Include(a => a.StudentCourse).FirstOrDefault(x => x.UnitId == activityEntity.UnitId);

                //Busqueda de entidad de studentsCourse para confirmar que si esta en la clase el estudiante
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

                //Se buscan las otras actividades en la unidad del estudiate
                var studentActivities = _context.StudentsActivitiesNotes.Include(x => x.Activity).Where(x => x.Activity.UnitId == activityEntity.UnitId && x.StudentId == activity.StudentId);

                //En esta lista se almacenaran los puntajes de estas actividades, para asi recalcular el promedio del parcial...
                var totalUnitPoints = new List<float>();

                //Dentro de studentUnit, se almacenara una sumatoria de las calificaciónes en bruto, tienen que pasarse de porcentaje a numero en bruto...

                //Si no hay otras actividades, la lista solo tendra la nota de esta actividad...
                if (studentActivities.Count() == 0)
                {
                    totalUnitPoints.Add((activity.Note / 100) * activityEntity.MaxScore);
                }
                else
                {
                    //Aqui se llena la lista para poder realizar la sumatoría...
                    foreach (var revisedActivity in studentActivities)
                    {
                        totalUnitPoints.Add((revisedActivity.Note / 100) * revisedActivity.Activity.MaxScore); // como la nota revisada es un porcentaje, se pasa a puntaje en bruto
                    }
                    //totalUnitPoints.Add((activity.Note / 100) * activityEntity.MaxScore); //Lo mismo para esta actividad recien revisada...

                }

                //Aqui se realiza la sumatoria...
                float newUnitScore = 0;
                newUnitScore = (totalUnitPoints.Sum());


                individualStudentUnit.UnitNote = newUnitScore;
                //Se actualiza la unidad de estudiante con la nueva calificación
                _context.StudentsUnits.Update(individualStudentUnit);
                await _context.SaveChangesAsync();

                //Procedimiento similar pero para el curso del estudiante...

                //En esa lista se almacenan los puntajes de unidad del estudiante...
                var totalPoints = new List<float>();
                foreach (var unit in studentUnitEntity)
                {

                    //Si no es oro, las sumatorias de studentUnit deben ser promediadas a corde a lo que vale la unidad para el curso
                    if (courseSetting.ScoreType != Constants.ScoreTypeConstant.GOLD_SCORE)
                    {
                        //Debe forsarse a ser float de esta forma para no dar problemas debido a que unit.maxScore
                        //permite null (para cursos con puntaje oro, aunque aqui no aplique siempre afecta el hecho de que los permita...
                        totalPoints.Add((float)((unit.UnitNote / 100) * unit.Unit.MaxScore));
                        //la formula es basicamente una forma de regla de 3, debido a que tanto ponderado como aritmetico permiten hasta 100 puntos dentro de su unidad...
                    }
                    else
                    {
                        //si es oro, solo hace una sumatoria de las sumatorias de unidades sin ponderar...
                        totalPoints.Add(unit.UnitNote);
                    };
                }


                //Se actualiza la nota en el curso del estudiante usando los valores almacenados...
                individualStudentUnit.StudentCourse.FinalNote = totalPoints.Sum();



                _context.StudentsCourses.Update(individualStudentUnit.StudentCourse);
                await _context.SaveChangesAsync();

                //Se busca la relacion actividad a estudiante para confirmar su existencia...
                var existingStudentActivity = _context.StudentsActivitiesNotes.Include(x => x.Student).FirstOrDefault(x => x.ActivityId == ActivityId && x.StudentId == activity.StudentId);



                //Si existe, se actualizan sus datos, sino, se crea...
                if (existingStudentActivity != null)
                {
                    //Se ingresa una entrada a la lista de espera del correo solo si cambio el feedback...
                    if (activity.Feedback != existingStudentActivity.Feedback)
                    {
                        emailRequest.Students.Add(new EmailFeedBackRequest.StudentInfo
                        {
                            Name = existingStudentActivity.Student.FirstName + " " + existingStudentActivity.Student.LastName,
                            Score = (activity.Note / 100) * activityEntity.MaxScore,
                            FeedBack = activity.Feedback,
                            Email = existingStudentActivity.Student.Email
                        });
                    }

                    existingStudentActivity.Note = activity.Note;
                    existingStudentActivity.Feedback = activity.Feedback;

                    activity.Id = existingStudentActivity.Id;
                    _context.StudentsActivitiesNotes.Update(existingStudentActivity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var student = _context.Students.FirstOrDefault(x => x.Id == activity.StudentId);
                    //Tambien se agrega una entrada si es la primera vez que se ingresa algo...
                    emailRequest.Students.Add(new EmailFeedBackRequest.StudentInfo
                    {
                        Name = student.FirstName + " " + student.LastName,
                        Score = (activity.Note / 100) * activityEntity.MaxScore,
                        FeedBack = activity.Feedback,
                        Email = student.Email
                    });

                    _context.StudentsActivitiesNotes.Add(activity);
                    await _context.SaveChangesAsync();
                }

            };

            if (emailRequest.Students.Count() > 0)
            {
                _emailQueue.Writer.TryWrite(emailRequest);
            }

            var studentActivityDto = _mapper.Map<List<StudentActivityNoteDto>>(studentActivityEntity);
            return new ResponseDto<List<StudentActivityNoteDto>>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.CREATE_SUCCESS,
                Data = studentActivityDto
            };

        }

        public async Task<ResponseDto<PaginationDto<List<ActivityDto>>>> GetStudentPendingsListAsync(
             Guid studentId,
             Guid courseId,
             int page = 1,
             int? pageSize = 10
             )
        {


            var userId = _auditService.GetUserId(); // Id de quien hace la petición

            var testStudent = _context.StudentsCourses.FirstOrDefault(x => x.StudentId == studentId && x.CourseId == courseId && x.CreatedBy == userId);


            
            if ( testStudent == null )
            {
                return new ResponseDto<PaginationDto<List<ActivityDto>>>
                {
                    StatusCode = 405,
                    Status = true,
                    Message = "El estudiante que ingresó no pertenece al curso, no existen o usted no esta autorizado."
                };
            }



            int MAX_PAGE_SIZE = 50;
            int currentPageSize = Math.Min(pageSize ?? PAGE_SIZE, MAX_PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;

            var activitiesQuery = _context.Activities
                .Where(a => a.CreatedBy == userId && a.Unit.CourseId == courseId &&!a.StudentNotes.Any(x => x.StudentId == studentId))
                .AsQueryable();


            // Total y paginado
            var totalActivities = await activitiesQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalActivities / currentPageSize);

            // Paginación y ordenación
            var activitiesEntity = await activitiesQuery
                .OrderByDescending(x => x.CreatedDate) // Ordenar por fecha de creación
                .Skip(startIndex)
                .Take(currentPageSize)
                .ToListAsync();

            // Mapear a DTO
            // centro retorna nulo siempre revisar 
            // depurar cada campo ->
            var activitiesDto = _mapper.Map<List<ActivityDto>>(activitiesEntity);

            return new ResponseDto<PaginationDto<List<ActivityDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_RECORDS_FOUND,
                Data = new PaginationDto<List<ActivityDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
                    TotalItems = totalActivities,
                    TotalPages = totalPages,
                    Items = activitiesDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        //En conjunto con GetStudentPendingsListAsync, este endpoint obtiene informacion relevante para el primero...
        public async Task<ResponseDto<StudentAndPendingsDto>> GetStudentPendingsInfoAsync(
            Guid studentId,
            Guid courseId
        )
        {

            var userId = _auditService.GetUserId(); 

            var studentEntity = await _context.StudentsCourses
                .Include(a => a.Student)
                .Include(a => a.Course)
                .ThenInclude(a => a.Center)
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.CourseId == courseId && a.CreatedBy == userId);

            if (studentEntity == null) 
            {
                return new ResponseDto<StudentAndPendingsDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = "El estudiante no pertenece al curso, no existen o no esta autorizado."
                };
            }

            var student = new StudentAndPendingsDto.StudentInfo
            {
                Id = studentId,
                FirstName = studentEntity.Student.FirstName,
                LastName = studentEntity.Student.LastName,
                Email = studentEntity.Student.Email,
                Status = studentEntity.IsActive
            };

            var course = new StudentAndPendingsDto.ClassInfo
            {
                Id = studentEntity.CourseId,
                ClassName = studentEntity.Course.Name,
                CenterId = studentEntity.Course.CenterId,
                CenterName = studentEntity.Course.Center.Name,
                CenterAbb = studentEntity.Course.Center.Abbreviation
            };

            var studentAndCourse = new StudentAndPendingsDto
            {
                Class = course,
                Student = student,
            };


            return new ResponseDto<StudentAndPendingsDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_RECORD_FOUND,
                Data = studentAndCourse
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

            //(Ken)
            //Se busca la unidad a la que pertenece esta actividad para hacer validaciones...
            var unitEntity = _context.Units.Include(x => x.Course).ThenInclude(x => x.CourseSetting).FirstOrDefault(z => z.Id == dto.UnitId);


            if (unitEntity is null)
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ACT_RECORD_NOT_FOUND
                };
            }
            //Estas dos variables son lostas de flotantes para hacer validaciones...
            //Se buscan las otras unidades enla unidad, que no sean extra, para confirmar que entre todas no se pasen de 100 que es lo maximo que una unidad no "oro" permite...
            var otherActivities = _context.Activities.Where(x => x.UnitId == dto.UnitId && !x.IsExtra).Select(x => x.MaxScore).ToList();
            //Lo mismo pero para puntos oro, esto es para verificar que entre todas las unidades DEL CURSO no se pasen del maximo del curso...
            var otherCourseActivities = _context.Activities.Include(x => x.Unit).Where(x => x.Unit.CourseId == unitEntity.CourseId && !x.IsExtra).Select(x => x.MaxScore).ToList();

            //Solo se considera que no se pasen si no son extra...
            if (!dto.IsExtra)
            {
                //Si no es oro, se confirma que no se pasen de 100...
                if (dto.MaxScore + otherActivities.Sum() > 100.00 && unitEntity.Course.CourseSetting.ScoreType != Constants.ScoreTypeConstant.GOLD_SCORE)
                {

                    return new ResponseDto<ActivityDto>
                    {
                        StatusCode = 405,
                        Status = false,
                        Message = "No puede agregar más puntos de el 100% de la unidad"
                    };
                }//Si son oro, se confirma que no se pasen de el maximo del curso...
                else if (dto.MaxScore > unitEntity.Course.CourseSetting.MaximumGrade - otherCourseActivities.Sum() && unitEntity.Course.CourseSetting.ScoreType == Constants.ScoreTypeConstant.GOLD_SCORE)
                {
                    return new ResponseDto<ActivityDto>
                    {
                        StatusCode = 405,
                        Status = false,
                        Message = "No puede agregar más puntos que el maximo de el curso"
                    };
                }
            }


            var activityEntity = _mapper.Map<ActivityEntity>(dto);
            _context.Activities.Add(activityEntity);
            await _context.SaveChangesAsync();

            //Cuando se agrega o modifica una actividad cuando ya se reviso a estudiantes, deben acomodarse todas esas entidades,
            //para que tomen en cuenta la nueva actividad o nuevo valor...

            //Se buscan los estudiantes del curso...
            var studentCourses = _context.StudentsCourses.Include(x => x.Course).Where(x => x.Course.Id == unitEntity.CourseId);

            //Estas son todas las relaciones estudiante a unidad que pertenescan a unidades de este curso..
            var fullStudentUnits = _context.StudentsUnits.Include(x => x.Unit).Include(x => x.StudentCourse).Where(x => x.StudentCourse.CourseId == unitEntity.CourseId).ToList();

            //En estas listas se almacenaran los studentUnit a agregar, los que se modificaron, y los student course, estas listas son para hacer addRange o updateRange
            List<StudentUnitEntity> newStudentUnitList = [];
            List<StudentUnitEntity> oldStudentUnitList = [];
            List<StudentCourseEntity> StudentCourseList = [];

            //Las studentUnit de la unidad a la que pertenece la actividad cambiaran, por lo que se les llama a todas las actividades de esa unidad para acoplar el nuevo valor...
            var allStudentActivities = _context.StudentsActivitiesNotes.Include(x => x.Activity).Where(x => x.Activity.UnitId == activityEntity.UnitId).ToList();

            //Por cada estudiante en el curso...
            foreach (var studentCourse in studentCourses)
            {
                //Se filtran las student unit de ese studentCourse en especifico...
                var studentUnits = fullStudentUnits.Where(x => x.StudentCourseId == studentCourse.Id).ToList();

                //Se guarda la studentUnit de esta actividad en especifico...
                var thisUnit = studentUnits.FirstOrDefault(x => x.UnitId == activityEntity.UnitId);


                //Si no existe se crea, para que este cuando se ñe revise al alumno...
                if (thisUnit == null)
                {

                    var newStudentUnit = new StudentUnitEntity
                    {
                        UnitNote = 0,
                        UnitNumber = activityEntity.Unit.UnitNumber,
                        UnitId = activityEntity.UnitId,
                        StudentCourseId = studentCourse.Id
                    };

                    newStudentUnitList.Add(newStudentUnit);

                    thisUnit = newStudentUnit;
                }

                //Se buscan las actividades de esta unidad el estudiante
                var studentActivities = allStudentActivities.Where(x => x.StudentId == studentCourse.StudentId).ToList();
                var totalUnitPoints = new List<float>();

                //Por cada actividad, si no es de puntaje "oro", Se pondera de 100 a su valor en el valor maximo real de la unidad, al igual que en el endpoint ReviewEntity, antes de este...
                //Aqui se llena la lista para poder realizar la sumatoría...
                foreach (var revisedActivity in studentActivities)
                {
                    totalUnitPoints.Add((revisedActivity.Note / 100) * revisedActivity.Activity.MaxScore); // como la nota revisada es un porcentaje, se pasa a puntaje en bruto
                }

                float newUnitScore = 0;


                newUnitScore = (totalUnitPoints.Sum());

                thisUnit.UnitNote = newUnitScore;
                oldStudentUnitList.Add(thisUnit);

                //Luego se usan los valores de todas las unidades del estudiante para actualizar la nota de curso...
                var totalPoints = new List<float>();
                foreach (var unit in studentUnits)
                {

                    //Si no es oro, las sumatorias de studentUnit deben ser promediadas a corde a lo que vale la unidad para el curso
                    if (unitEntity.Course.CourseSetting.ScoreType != Constants.ScoreTypeConstant.GOLD_SCORE)
                    {
                        //Debe forsarse a ser float de esta forma para no dar problemas debido a que unit.maxScore
                        //permite null (para cursos con puntaje oro, aunque aqui no aplique siempre afecta el hecho de que los permita...
                        totalPoints.Add((float)((unit.UnitNote / 100) * unit.Unit.MaxScore));
                        //la formula es basicamente una forma de regla de 3, debido a que tanto ponderado como aritmetico permiten hasta 100 puntos dentro de su unidad...
                    }
                    else
                    {
                        //si es oro, solo hace una sumatoria de las sumatorias de unidades sin ponderar...
                        totalPoints.Add(unit.UnitNote);
                    };
                }

                //Se actualiza la nota en el curso del estudiante usando los valores de la lista...
                studentCourse.FinalNote = totalPoints.Sum();


                StudentCourseList.Add(studentCourse);

            }

            //Los add y update range de las entidades...
            _context.StudentsUnits.AddRange(newStudentUnitList);
            await _context.SaveChangesAsync();

            _context.StudentsUnits.UpdateRange(oldStudentUnitList);
            await _context.SaveChangesAsync();

            _context.StudentsCourses.UpdateRange(StudentCourseList);
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

            ////(Ken)
            ////Para impedir cambios de unidad...
            //if (activityEntity.UnitId != dto.UnitId)
            //{
            //    dto.UnitId = activityEntity.UnitId;
            //}

            //AL igual que en createAsync, se usa unitEntity para hacer validaciones...
            var unitEntity = _context.Units.Include(x => x.Course).ThenInclude(x => x.CourseSetting).FirstOrDefault(z => z.Id == activityEntity.UnitId);


            if (unitEntity is null)
            {
                return new ResponseDto<ActivityDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.ACT_RECORD_NOT_FOUND
                };
            }

            //Estas dos variables son lostas de flotantes para hacer validaciones...
            //Se buscan las otras unidades en la unidad, que no sean extra, para confirmar que entre todas no se pasen de 100 que es lo maximo que una unidad no "oro" permite...
            var otherActivities = _context.Activities.Where(x => x.UnitId == activityEntity.UnitId && !x.IsExtra).Select(x => x.MaxScore).ToList();
            //Lo mismo pero para puntos oro, esto es para verificar que entre todas las unidades DEL CURSO no se pasen del maximo del curso...
            var otherCourseActivities = _context.Activities.Include(x => x.Unit).Where(x => x.Unit.CourseId == unitEntity.CourseId && !x.IsExtra).Select(x => x.MaxScore).ToList();


            //Solo se considera que no se pasen si no son extra...
            if (!dto.IsExtra)
            {
                //Si no es oro, se confirma que no se pasen de 100...
                if (dto.MaxScore + otherActivities.Sum() > 100.00 && unitEntity.Course.CourseSetting.ScoreType != Constants.ScoreTypeConstant.GOLD_SCORE)
                {

                    return new ResponseDto<ActivityDto>
                    {
                        StatusCode = 405,
                        Status = false,
                        Message = "No puede agregar más puntos de el 100% de la unidad"
                    };
                }//Si es oro, confirma pero de que no se pase del maximo de puntos de el curso...
                else if (dto.MaxScore > unitEntity.Course.CourseSetting.MaximumGrade - otherCourseActivities.Sum() && unitEntity.Course.CourseSetting.ScoreType == Constants.ScoreTypeConstant.GOLD_SCORE)
                {
                    return new ResponseDto<ActivityDto>
                    {
                        StatusCode = 405,
                        Status = false,
                        Message = "No puede agregar más puntos que el maximo de el curso"
                    };
                }
            }

            _mapper.Map(dto, activityEntity);
            _context.Activities.Update(activityEntity);
            await _context.SaveChangesAsync();



            //Se buscan los estudiantes del curso...
            var studentCourses = _context.StudentsCourses.Include(x => x.Course).Where(x => x.Course.Id == unitEntity.CourseId);

            //Estas son todas las relaciones estudiante a unidad que pertenescan a unidades de este curso..
            var fullStudentUnits = _context.StudentsUnits.Include(x => x.Unit).Include(x => x.StudentCourse).Where(x => x.StudentCourse.CourseId == unitEntity.CourseId).ToList();

            //En estas listas se almacenaran los studentUnit a agregar, los que se modificaron, y los student course, estas listas son para hacer addRange o updateRange
            List<StudentUnitEntity> newStudentUnitList = [];
            List<StudentUnitEntity> oldStudentUnitList = [];
            List<StudentCourseEntity> StudentCourseList = [];

            //Las studentUnit de la unidad a la que pertenece la actividad cambiaran, por lo que se les llama a todas las actividades de esa unidad para acoplar el nuevo valor...
            var allStudentActivities = _context.StudentsActivitiesNotes.Include(x => x.Activity).Where(x => x.Activity.UnitId == activityEntity.UnitId).ToList();

            //Por cada alumno en el curso...
            foreach (var studentCourse in studentCourses)
            {
                //StudentUnit de este alumno
                var studentUnits = fullStudentUnits.Where(x => x.StudentCourseId == studentCourse.Id).ToList();

                //La unidad de esta actividad y alumno
                var thisUnit = studentUnits.FirstOrDefault(x => x.UnitId == activityEntity.UnitId);


                //Si no existe la studentUnit se crea para que cuando se le revise al alumno pueda usarse la entidad directamente...
                if (thisUnit == null)
                {

                    var newStudentUnit = new StudentUnitEntity
                    {
                        UnitNote = 0,
                        UnitNumber = activityEntity.Unit.UnitNumber,
                        UnitId = activityEntity.UnitId,
                        StudentCourseId = studentCourse.Id
                    };

                    newStudentUnitList.Add(newStudentUnit);

                    thisUnit = newStudentUnit;
                }

                //Actividades de este estudiante
                var studentActivities = allStudentActivities.Where(x => x.StudentId == studentCourse.StudentId).ToList();
                var totalUnitPoints = new List<float>();

                foreach (var revisedActivity in studentActivities)
                {
                    totalUnitPoints.Add((revisedActivity.Note / 100) * revisedActivity.Activity.MaxScore); // como la nota revisada es un porcentaje, se pasa a puntaje en bruto
                }

                float newUnitScore = 0;


                newUnitScore = (totalUnitPoints.Sum());

                thisUnit.UnitNote = newUnitScore;
                oldStudentUnitList.Add(thisUnit);

                //Se suman los puntajes del alumno en unidades para sacar el total del curso...
                var totalPoints = new List<float>();
                foreach (var unit in studentUnits)
                {

                    //Si no es oro, las sumatorias de studentUnit deben ser promediadas a corde a lo que vale la unidad para el curso
                    if (unitEntity.Course.CourseSetting.ScoreType != Constants.ScoreTypeConstant.GOLD_SCORE)
                    {
                        //Debe forsarse a ser float de esta forma para no dar problemas debido a que unit.maxScore
                        //permite null (para cursos con puntaje oro, aunque aqui no aplique siempre afecta el hecho de que los permita...
                        totalPoints.Add((float)((unit.UnitNote / 100) * unit.Unit.MaxScore));
                        //la formula es basicamente una forma de regla de 3, debido a que tanto ponderado como aritmetico permiten hasta 100 puntos dentro de su unidad...
                    }
                    else
                    {
                        //si es oro, solo hace una sumatoria de las sumatorias de unidades sin ponderar...
                        totalPoints.Add(unit.UnitNote);
                    };
                }


                //Se actualiza la nota en el curso del estudiante es igual a la suma de studentUnit...
                studentCourse.FinalNote = totalPoints.Sum();


                StudentCourseList.Add(studentCourse);

            }

            _context.StudentsUnits.AddRange(newStudentUnitList);
            await _context.SaveChangesAsync();

            _context.StudentsUnits.UpdateRange(oldStudentUnitList);
            await _context.SaveChangesAsync();

            _context.StudentsCourses.UpdateRange(StudentCourseList);
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
            var unitEntity = _context.Units.Include(x => x.Course).ThenInclude(x => x.CourseSetting).FirstOrDefault(z => z.Id == activityEntity.UnitId);

            var revisedActivities = _context.StudentsActivitiesNotes.Where(a => a.ActivityId == activityEntity.Id);

            _context.StudentsActivitiesNotes.RemoveRange(revisedActivities);
            await _context.SaveChangesAsync();
            //Se buscan los estudiantes del curso...
            var studentCourses = _context.StudentsCourses.Include(x => x.Course).Where(x => x.Course.Id == unitEntity.CourseId);

            //Estas son todas las relaciones estudiante a unidad que pertenescan a unidades de este curso..
            var fullStudentUnits = _context.StudentsUnits.Include(x => x.Unit).Include(x => x.StudentCourse).Where(x => x.StudentCourse.CourseId == unitEntity.CourseId).ToList();

            //En estas listas se almacenaran los studentUnit a agregar, los que se modificaron, y los student course, estas listas son para hacer addRange o updateRange
            List<StudentUnitEntity> oldStudentUnitList = [];
            List<StudentCourseEntity> StudentCourseList = [];

            //Las studentUnit de la unidad a la que pertenece la actividad cambiaran, por lo que se les llama a todas las actividades de esa unidad para acoplar el nuevo valor...
            var allStudentActivities = _context.StudentsActivitiesNotes.Include(x => x.Activity).Where(x => x.Activity.UnitId == activityEntity.UnitId).ToList();

            //Por cada alumno en el curso...
            foreach (var studentCourse in studentCourses)
            {
                //StudentUnit de este alumno
                var studentUnits = fullStudentUnits.Where(x => x.StudentCourseId == studentCourse.Id).ToList();

                //La unidad de esta actividad y alumno
                var thisUnit = studentUnits.FirstOrDefault(x => x.UnitId == activityEntity.UnitId);


                //Actividades de este estudiante
                var studentActivities = allStudentActivities.Where(x => x.StudentId == studentCourse.StudentId).ToList();
                var totalUnitPoints = new List<float>();

                foreach (var revisedActivity in studentActivities)
                {
                    totalUnitPoints.Add((revisedActivity.Note / 100) * revisedActivity.Activity.MaxScore); // como la nota revisada es un porcentaje, se pasa a puntaje en bruto
                }

                float newUnitScore = 0;


                newUnitScore = (totalUnitPoints.Sum());

                thisUnit.UnitNote = newUnitScore;
                oldStudentUnitList.Add(thisUnit);

                //Se suman los puntajes del alumno en unidades para sacar el total del curso...
                var totalPoints = new List<float>();
                foreach (var unit in studentUnits)
                {

                    //Si no es oro, las sumatorias de studentUnit deben ser promediadas a corde a lo que vale la unidad para el curso
                    if (unitEntity.Course.CourseSetting.ScoreType != Constants.ScoreTypeConstant.GOLD_SCORE)
                    {
                        //Debe forsarse a ser float de esta forma para no dar problemas debido a que unit.maxScore
                        //permite null (para cursos con puntaje oro, aunque aqui no aplique siempre afecta el hecho de que los permita...
                        totalPoints.Add((float)((unit.UnitNote / 100) * unit.Unit.MaxScore));
                        //la formula es basicamente una forma de regla de 3, debido a que tanto ponderado como aritmetico permiten hasta 100 puntos dentro de su unidad...
                    }
                    else
                    {
                        //si es oro, solo hace una sumatoria de las sumatorias de unidades sin ponderar...
                        totalPoints.Add(unit.UnitNote);
                    };
                }


                //Se actualiza la nota en el curso del estudiante es igual a la suma de studentUnit...
                studentCourse.FinalNote = totalPoints.Sum();


                StudentCourseList.Add(studentCourse);

            }


            _context.StudentsUnits.UpdateRange(oldStudentUnitList);
            await _context.SaveChangesAsync();

            _context.StudentsCourses.UpdateRange(StudentCourseList);
            await _context.SaveChangesAsync();
            _context.Activities.Remove(activityEntity);
            await _context.SaveChangesAsync();
            return new ResponseDto<ActivityDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_DELETE_SUCCESS
            };
        }

        public async Task<ResponseDto<PaginationDto<List<ActivityResponseDto>>>> GetAllActivitiesByClassAsync(
            Guid id,
            string searchTerm = "",
            int page = 1,
            int? pageSize = 10,
            Guid? tagActivityId = null,
            Guid? unitId = null,
            string typeActivities = "ALL",
            string isExtraFilter = "ALL"
        )
        {
            var allowedValues = new HashSet<string> { "ALL", "PENDING", "DONE" };
            var allowedExtraTypes = new HashSet<string> { "ALL", "TRUE", "FALSE" };
            if (!allowedValues.Contains(typeActivities.ToUpper()))
            {
                return new ResponseDto<PaginationDto<List<ActivityResponseDto>>>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "typeActivities solo puede ser  [ ALL , PENDING , DONE ]",
                    Data = null,
                };
            }

            if (!allowedExtraTypes.Contains(isExtraFilter.ToUpper()))
            {
                return new ResponseDto<PaginationDto<List<ActivityResponseDto>>>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "isExtra solo puede ser [ALL, TRUE, FALSE]",
                    Data = null
                };
            }

            var userId = _auditService.GetUserId(); 

            int MAX_PAGE_SIZE = 50;
            int currentPageSize = Math.Min(pageSize ?? PAGE_SIZE, MAX_PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;

            DateTime now = DateTime.UtcNow;
            var query = _context.Activities
                 .AsNoTracking()
                 .Where(a => a.Unit.CourseId == id && a.CreatedBy == userId)
                 .Include(a => a.Unit)
                 .Include(a => a.TagActivity)
                 .Include(a => a.StudentNotes)
                 .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(searchTerm.ToLower()));
            }

            if (tagActivityId.HasValue)
            {
                query = query.Where(a => a.TagActivityId == tagActivityId.Value);
            }

            if (unitId.HasValue)
            {
                query = query.Where(a => a.UnitId == unitId.Value);
            }

            if (typeActivities.ToUpper() == "PENDING")
            {
                query = query.Where(a =>
                    a.QualificationDate < now &&
                    a.StudentNotes.Select(n => n.StudentId).Distinct().Count() <
                    _context.StudentsCourses.Count(sc => sc.CourseId == a.Unit.CourseId && sc.IsActive));
            }
            if (typeActivities.ToUpper() == "DONE")
            {
                query = query.Where(a =>
                    a.QualificationDate < now &&
                    a.StudentNotes.Select(n => n.StudentId).Distinct().Count() >=
                    _context.StudentsCourses.Count(sc => sc.CourseId == a.Unit.CourseId && sc.IsActive));
            }

            // Filtro isExtra
            if (isExtraFilter.ToUpper() == "TRUE")
                query = query.Where(a => a.IsExtra);
            if (isExtraFilter.ToUpper() == "FALSE")
                query = query.Where(a => !a.IsExtra);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);

            var data = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip(startIndex)
                .Take(currentPageSize)
                .Select(a => new ActivityResponseDto
                {
                    ActivityId = a.Id,
                    ActivityName = a.Name ?? "Sin nombre",
                    ActivityDescription = a.Description ?? "",
                    QualificationDate = a.QualificationDate,
                    MaxScore = a.MaxScore,
                    IsExtra = a.IsExtra,
                    UnitId = a.UnitId,
                    UnitNumber = a.Unit.UnitNumber,
                    TagActivityId = a.TagActivityId
                })
                .ToListAsync();

            return new ResponseDto<PaginationDto<List<ActivityResponseDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_RECORDS_FOUND,
                Data = new PaginationDto<List<ActivityResponseDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = data,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }
    }
}
