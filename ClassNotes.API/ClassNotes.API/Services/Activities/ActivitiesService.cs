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
            IAuditService auditService,
            IConfiguration configuration
        )
        {
            _context = context;
            _mapper = mapper;
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Activities");
            _auditService = auditService;
        }

        // Traer todas las actividades (Paginadas)
        public async Task<ResponseDto<PaginationDto<List<ActivityDto>>>> GetActivitiesListAsync(
            string searchTerm = "",
            int page = 1
        )
        {
            int startIndex = (page - 1) * PAGE_SIZE;

            var activitiesQuery = _context.Activities.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                activitiesQuery = activitiesQuery
                    .Where(x => (x.Name + " " + x.Name)
                    .ToLower().Contains(searchTerm.ToLower()));
            }

            int totalActivities = await activitiesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalActivities / PAGE_SIZE);

            var activitiesEntity = await activitiesQuery
                .OrderByDescending(x => x.CreatedDate)
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var activitiesDto = _mapper.Map<List<ActivityDto>>(activitiesEntity);

            return new ResponseDto<PaginationDto<List<ActivityDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.ACT_RECORDS_FOUND,
                Data = new PaginationDto<List<ActivityDto>>
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

        public async Task<ResponseDto<StudentActivityNoteDto>> ReviewActivityAsync(StudentActivityNoteCreateDto dto)
        {
            var studentActivityEntity = _mapper.Map<StudentActivityNoteEntity>(dto);

            var activityEntity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == dto.ActivityId );

            var studentUnits =  _context.StudentsUnits.Include(a => a.StudentCourse).Where(x => x.StudentCourse.StudentId == studentActivityEntity.StudentId);

            var studentUnitEntity = await studentUnits.FirstOrDefaultAsync(a => a.UnitId == activityEntity.UnitId);

         

            if (studentUnitEntity == null || activityEntity == null)
            {
                return new ResponseDto<StudentActivityNoteDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }


            if (dto.Note > activityEntity.MaxScore  || dto.Note < 0)
            {
                return new ResponseDto<StudentActivityNoteDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Se ingresó una calificación no valida."
                };
            }

            var activityCount = await _context.Activities.Where(x => x.UnitId == studentUnitEntity.UnitId && x.IsExtra == false).CountAsync();
           
            if (activityCount == 0)
            {
                return new ResponseDto<StudentActivityNoteDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }


            if(!activityEntity.IsExtra){

                var newUnitScore = studentUnitEntity.UnitNote+((dto.Note - studentUnitEntity.UnitNote) / activityCount);

                var totalPoints = new List<float>();

                foreach (var unit in studentUnits)
                {
                    totalPoints.Add(unit.UnitNote);
                }

                studentUnitEntity.StudentCourse.FinalNote = totalPoints.Sum() /totalPoints.Count();
                studentUnitEntity.UnitNote = newUnitScore;

                _context.StudentsCourses.Update(studentUnitEntity.StudentCourse);
                await _context.SaveChangesAsync(); //cambiaaaaaaaaaaaaaaaaaaar

                _context.StudentsUnits.Update(studentUnitEntity);
                await _context.SaveChangesAsync();
            }


            _context.StudentsActivitiesNotes.Add(studentActivityEntity);
            await _context.SaveChangesAsync();



            var studentActivityDto = _mapper.Map<StudentActivityNoteDto>(studentActivityEntity);
            return new ResponseDto<StudentActivityNoteDto>
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
            var activityEntity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id);
            if (activityEntity == null)
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

            var activityEntity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id);
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
            var activityEntity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id);
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
