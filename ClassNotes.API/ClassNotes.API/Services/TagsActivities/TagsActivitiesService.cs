using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.TagsActivities;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.TagsActivities
{
	public class TagsActivitiesService : ITagsActivitiesService
	{
		private readonly ClassNotesContext _context;
		private readonly IAuditService _auditService;
		private readonly IMapper _mapper;
		private readonly ILogger _logger;
		private readonly int PAGE_SIZE;

		public TagsActivitiesService(
			ClassNotesContext context, 
			IAuditService auditService, 
			IMapper mapper,
			ILogger<TagsActivitiesService> logger,
			IConfiguration configuration
			)
		{
			_context = context;
			_auditService = auditService;
			_mapper = mapper;
			_logger = logger;
			PAGE_SIZE = configuration.GetValue<int>("PageSize");
		}

		// AM: Metodo para obtener todas las tags en forma de paginación
		public async Task<ResponseDto<PaginationDto<List<TagActivityDto>>>> GetTagsListAsync(string searchTerm = "", int page = 1)
		{
			int startIndex = (page - 1) * PAGE_SIZE;

			// AM: ID del usuario en sesión
			var userId = _auditService.GetUserId(); 

			// AM: Filtrar unicamente las tags que pertenecen al usuario
			var tagsQuery = _context.TagsActivities.Where(c => c.CreatedBy == userId).AsQueryable();

			// AM: Buscar por nombre de la tag
			if (!string.IsNullOrEmpty(searchTerm))
			{
				tagsQuery = tagsQuery.Where(t => t.Name.ToLower().Contains(searchTerm.ToLower()));
			}

			int totalItems = await tagsQuery.CountAsync();
			int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

			// AM: Aplicar paginacion 
			var tagsEntities = await tagsQuery
				.OrderByDescending(t => t.CreatedDate) // AM: Ordenar por fecha de creación (Más viejos primero) 
				.Skip(startIndex)
				.Take(PAGE_SIZE)
				.ToListAsync();

			// AM: Mapear a DTO para la respuesta
			var tagsDto = _mapper.Map<List<TagActivityDto>>(tagsEntities);

			return new ResponseDto<PaginationDto<List<TagActivityDto>>>
			{
				StatusCode = 200,
				Status = true,
				Message = totalItems == 0 ? MessagesConstant.TA_RECORD_NOT_FOUND : MessagesConstant.TA_RECORDS_FOUND, // AM: Si no encuentra items mostrar el mensaje correcto
				Data = new PaginationDto<List<TagActivityDto>>
				{
					CurrentPage = page,
					PageSize = PAGE_SIZE,
					TotalItems = totalItems,
					TotalPages = totalPages,
					Items = tagsDto,
					HasPreviousPage = page > 1,
					HasNextPage = page < totalPages
				}
			};
		}

		// AM: Metodo para obtener información de una Tag por su id
		public async Task<ResponseDto<TagActivityDto>> GetTagByIdAsync(Guid id)
		{
			// AM: Id del usuario en sesión
			var userId = _auditService.GetUserId();

			// AM: Validar existencia y filtrar por CreatedBy
			var tagEntity = await _context.TagsActivities.FirstOrDefaultAsync(t => t.Id == id && t.CreatedBy == userId); 
			if (tagEntity == null)
			{
				return new ResponseDto<TagActivityDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.TA_RECORD_NOT_FOUND
				};
			}

			// AM: Mapear a DTO para la respuesta
			var tagDto = _mapper.Map<TagActivityDto>(tagEntity);

			return new ResponseDto<TagActivityDto>
			{
				StatusCode = 200,
				Status = true,
				Message = MessagesConstant.TA_RECORD_FOUND,
				Data = tagDto
			};
		}

		// AM: Metodo para crear una nueva Tag
		public async Task<ResponseDto<TagActivityDto>> CreateTagAsync(TagActivityCreateDto dto)
		{
			try
			{
				/* Las validaciones de seguridad necesarias se realizan en el DTO de TagActivityCreateDto */

				// AM: Crear la nueva tag
				var tagEntity = _mapper.Map<TagActivityEntity>(dto);

				// AM: el teacher id corresponde al usuario en sesión
				tagEntity.TeacherId = _auditService.GetUserId();

				// AM: Guardar cambios
				_context.TagsActivities.Add(tagEntity);
				await _context.SaveChangesAsync();

				// AM: Mapear Entity a Dto para la respuesta
				var tagDto = _mapper.Map<TagActivityDto>(tagEntity);

				return new ResponseDto<TagActivityDto>
				{
					StatusCode = 201,
					Status = true,
					Message = MessagesConstant.TA_CREATE_SUCCESS,
					Data = tagDto
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, MessagesConstant.TA_CREATE_ERROR);
				return new ResponseDto<TagActivityDto>
				{
					StatusCode = 500,
					Status = false,
					Message = MessagesConstant.TA_CREATE_ERROR
				};
			}
		}

		public async Task<ResponseDto<TagActivityDto>> UpdateTagAsync(TagActivityEditDto dto, Guid id)
		{
			try
			{
				throw new NotImplementedException();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, MessagesConstant.TA_UPDATE_ERROR);
				return new ResponseDto<TagActivityDto>
				{
					StatusCode = 500,
					Status = false,
					Message = MessagesConstant.TA_UPDATE_ERROR
				};
			}
		}

		public async Task<ResponseDto<TagActivityDto>> DeleteTagAsync(Guid id)
		{
			try
			{
				throw new NotImplementedException();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, MessagesConstant.TA_DELETE_ERROR);
				return new ResponseDto<TagActivityDto>
				{
					StatusCode = 500,
					Status = false,
					Message = MessagesConstant.TA_DELETE_ERROR
				};
			}
		}
	}
}
