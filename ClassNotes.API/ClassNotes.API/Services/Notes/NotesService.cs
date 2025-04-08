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
using System.Linq;


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

    }
}
