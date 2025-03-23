using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Activities
{
    // --------------------- CP --------------------- //
    public class ActivitiesService : IActivitiesService
    {
        private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly int PAGE_SIZE;
        private readonly IAuditService _auditService;

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
