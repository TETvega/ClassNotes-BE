using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet.Actions;
using CloudinaryInstance = CloudinaryDotNet.Cloudinary;
using ClassNotes.API.Dtos.CourseNotes;
using System.Diagnostics;
using MailKit.Search;
using ClassNotes.API.Dtos.Centers;


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
            PAGE_SIZE = configuration.GetValue<int>("PageSize");
            this._context = context;
        }


        public async Task<ResponseDto<PaginationDto<List<StudentUnitNoteDto>>>> GetStudentUnitsNotesAsync(Guid studentId, Guid courseId, int page = 1)
        {

            int startIndex = (page - 1) * PAGE_SIZE;

            var userId = _auditService.GetUserId();

            //Busca todas las entidades de unidad estudiante
            var studentUnitsQuery = _context.StudentsUnits
                    .Include(c => c.StudentCourse)
                    .Where(c=>  c.StudentCourse.StudentId == studentId && c.StudentCourse.CourseId == courseId) 
                    .AsQueryable();

            int totalItems = await studentUnitsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

          
            var studentUnitEntities = await studentUnitsQuery
                .OrderByDescending(n => n.UnitNumber) 
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

            var studentCoursesQuery = _context.StudentsCourses
                    .Where(c => c.CourseId == courseId)
                    .AsQueryable();

            //Busca el curso especifcado por el id
            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == courseId );

            if (course.CreatedBy != userId)
            {
                return new ResponseDto<PaginationDto<List<StudentTotalNoteDto>>>
                {
                    StatusCode = 401,
                    Status = false,
                    Message = "No esta autorizado para ver estos registros."
                };
            }

            //Busca el setting, para poder encontrar el max score
            var courseSetting = await _context.CoursesSettings.FirstOrDefaultAsync(x => x.Id == course.SettingId && x.CreatedBy == userId);

            int totalItems = await studentCoursesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

            //Listas que guardaran los puntajes  maximos
          
            var totalPoints = new List<float>();

            //Busca las actividades revisadas de estudiantes que son extra en base al curso, y busca las actividades no extra del curso para saber su max score
             var courseActivities = _context.Activities.Where(x => x.Unit.CourseId == courseId && x.IsExtra ==false);

            //Hace una sumatoria de las notas maximas en cada unidad para saber la cantidad de puntos maxima en un curso
            foreach (var activity in courseActivities)
            {
                totalPoints.Add(activity.MaxScore);
            }


            var studentNotesEntities = await studentCoursesQuery
                .OrderByDescending(n => n.CreatedDate)
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var studentNoteDto = _mapper.Map<List<StudentTotalNoteDto>>(studentNotesEntities);

            //por cada estudiante en el curso, calcula la nota poderada en base con la regla de 3, usando la nota maxima obtenida con una sumatoria de totalPoints y considerando las actividades extra reviadas
            //posible cambio o verificacion porque, si la nota total evaluada por el docente es menor al valor de maximun grade, da un dato mayor a maximun grade
            studentNoteDto.ForEach(studentNote => {
                var totalExtraPoints = _context.StudentsActivitiesNotes
                    .Where(x => x.Activity.Unit.CourseId == courseId&& x.Activity.IsExtra == true && x.StudentId == studentNote.StudentId).Sum(x => x.Note); 


                studentNote.AveragedNote = (courseSetting.MaximumGrade/((totalPoints.Sum()/totalPoints.Count()) / studentNote.FinalNote))+totalExtraPoints;
          
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
                    .Where(c => c.Activity.Unit.CourseId == courseId);

            var activities = _context.Activities.Include(u=> u.Unit).Where(x=> x.Unit.CourseId==courseId);

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

    }
}
