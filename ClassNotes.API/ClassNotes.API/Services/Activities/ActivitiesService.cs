using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Activities
{
    // --------------------- CP --------------------- //
    public class ActivitiesService : IActivitiesService
    {
        private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly int PAGE_SIZE;

        public ActivitiesService(
            ClassNotesContext context,
            IMapper mapper,
            IConfiguration configuration
        )
        {
            _context = context;
            _mapper = mapper;
            PAGE_SIZE = configuration.GetValue<int>("PageSize");
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
                Message = MessagesConstant.RECORDS_FOUND,
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
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }
            var activityDto = _mapper.Map<ActivityDto>(activityEntity);
            return new ResponseDto<ActivityDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORD_FOUND,
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
                    Message = "La fecha de calificación no puede ser menor a la fecha actual."
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
                Message = MessagesConstant.CREATE_SUCCESS,
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
                    Message = "La fecha de calificación no puede ser menor a la fecha actual."
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
                    Message = MessagesConstant.RECORD_NOT_FOUND
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
                Message = MessagesConstant.UPDATE_SUCCESS,
                Data = activityDto
            };
        }

        public Task<ResponseDto<ActivityDto>> DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
