using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
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

		public Task<ResponseDto<ActivityDto>> GetActivityByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseDto<ActivityDto>> CreateAsync(ActivityCreateDto dto)
        {
            throw new NotImplementedException();
        }

		public Task<ResponseDto<ActivityDto>> EditAsync(ActivityEditDto dto, Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseDto<ActivityDto>> DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }   
    }
}
