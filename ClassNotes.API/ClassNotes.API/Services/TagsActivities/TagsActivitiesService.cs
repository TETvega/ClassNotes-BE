using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.TagsActivities;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
			PAGE_SIZE = configuration.GetValue<int>("PageSize:Tags"); // AM: Aquí obtenemos el pageSize correspondiente de Tags
		}

		// AM: Metodo para obtener todas las tags en forma de paginación
		public async Task<ResponseDto<PaginationDto<List<TagActivityDto>>>> GetTagsListAsync(string searchTerm = "", int page = 1)
		{
			//int startIndex = (page - 1) * PAGE_SIZE;

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
			//int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

			// AM: Aplicar paginacion 
			var tagsEntities = await tagsQuery
				.OrderByDescending(t => t.CreatedDate) // AM: Ordenar por fecha de creación (Más viejos primero) 
				//.Skip(startIndex)
				//.Take(PAGE_SIZE)
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
					PageSize = totalItems,
					TotalItems = totalItems,
					TotalPages = 1,
					Items = tagsDto,
					HasPreviousPage = page > 1,
					HasNextPage = page < 1
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
                // HR: Id del usuario en sesión
                var userId = _auditService.GetUserId();
                /* Las validaciones de seguridad necesarias se realizan en el DTO de TagActivityCreateDto */
                var tagCount = await _context.TagsActivities.CountAsync(t => t.TeacherId == userId);
                if (tagCount >= 25)
                {
                    return new ResponseDto<TagActivityDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "No puedes crear más de 25 etiquetas."
                    };
                }
                if (dto.Name.ToLower().Trim() == "undefined")
                {
                    return new ResponseDto<TagActivityDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "No se puede asignar el nombre 'Undefined' a otra etiqueta."
                    };
                }
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

		// AM: Metodo para editar una Tag existente
		public async Task<ResponseDto<TagActivityDto>> UpdateTagAsync(TagActivityEditDto dto, Guid id)
		{
			try
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
                if (tagEntity.Name.ToLower().Trim() == "undefined")
                {
                    return new ResponseDto<TagActivityDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "No se puede editar la etiqueta por defecto 'Undefined'."
                    };
                }
                if (dto.Name == "Undefined")
                {
                    return new ResponseDto<TagActivityDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "No se puede asignar el nombre 'Undefined' a otra etiqueta."
                    };
                }

                // AM: Mapear el DTO a Entity
                _mapper.Map(dto, tagEntity);
				// AM: Actualizar y guardar cambios
				_context.TagsActivities.Update(tagEntity);
				await _context.SaveChangesAsync();

				// AM: Mapear Entity a Dto para la respuesta
				var tagDto = _mapper.Map<TagActivityDto>(tagEntity);

				return new ResponseDto<TagActivityDto>
				{
					StatusCode = 200,
					Status = true,
					Message = MessagesConstant.TA_UPDATE_SUCCESS,
					Data = tagDto
				};
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

        // AM: Metodo para eliminar una tag completamente de la base de datos
        public async Task<ResponseDto<List<TagActivityDto>>> DeleteTagAsync(List<Guid> listGuidsTags)
        {
            try
            {
                // AM: Obtener el ID del usuario en sesión
                var userId = _auditService.GetUserId();

                // AM: Obtener todas las etiquetas del usuario en la lista proporcionada
                var userTags = await _context.TagsActivities
                    .Where(t => listGuidsTags.Contains(t.Id) && t.CreatedBy == userId)
                    .ToListAsync();

                // AM: Validar que todas las etiquetas pertenezcan al usuario
                if (userTags.Count != listGuidsTags.Count)
                {
                    return new ResponseDto<List<TagActivityDto>>
					{
						StatusCode = 403, // Código de error por permisos insuficientes
						Status = false,
						Message = $"{MessagesConstant.UNAUTHORIZED_DELETE} && {MessagesConstant.RECORD_NOT_FOUND}"

					};
                }

                // AM: Buscar la etiqueta por defecto "Undefined"
                var defaultTag = await _context.TagsActivities
                    .FirstOrDefaultAsync(t => t.Name == "Undefined" && t.CreatedBy == userId);

                if (defaultTag == null)
                {
                    return new ResponseDto<List<TagActivityDto>>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = "No se encontró la etiqueta por defecto 'Undefined'."
                    };
                }
                if (userTags.Any(t => t.Name == "Undefined"))
                {
                    return new ResponseDto<List<TagActivityDto>>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "No se puede eliminar la etiqueta por defecto 'Undefined'."
                    };
                }


                // AM: Verificar si alguna etiqueta está en uso y reasignarlas
                var tagIds = userTags.Select(t => t.Id).ToList();
                var activitiesToUpdate = await _context.Activities
                    .Where(a => tagIds.Contains(a.TagActivityId))
                    .ToListAsync();

                foreach (var activity in activitiesToUpdate)
                {
                    activity.TagActivityId = defaultTag.Id;
                }

                // AM: Eliminar las etiquetas del usuario
                _context.TagsActivities.RemoveRange(userTags);

                // AM: Guardar cambios en la base de datos
                await _context.SaveChangesAsync();

                // AM: Mapear la lista eliminada a DTOs
                var tagDtos = _mapper.Map<List<TagActivityDto>>(userTags);

                return new ResponseDto<List<TagActivityDto>>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = MessagesConstant.TA_DELETE_SUCCESS,
                    Data = tagDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MessagesConstant.TA_DELETE_ERROR);
                return new ResponseDto<List<TagActivityDto>>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = MessagesConstant.TA_DELETE_ERROR
                };
            }
        }

        // AM: Metodo para crear un conjunto de tags predeterminadas
        public async Task<ResponseDto<List<TagActivityDto>>> CreateDefaultTagsAsync(string userId)
		{
			try
			{
				// AM: Cargar las tags predeterminadas desde el archivo JSON
				var jsonFilePath = "SeedData/default_tags_activities.json";
				var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
				var defaultTags = JsonConvert.DeserializeObject<List<TagActivityEntity>>(jsonContent);

				// AM: Asignar cada tag al nuevo usuario
				foreach (var tag in defaultTags)
				{
					tag.Id = Guid.NewGuid();
					tag.TeacherId = userId;
					tag.CreatedDate = DateTime.Now;
					tag.UpdatedDate = DateTime.Now;
					tag.CreatedBy = userId;
					tag.UpdatedBy = userId;

					// AM: Agregar la tag al contexto
					await _context.TagsActivities.AddAsync(tag);
				}

				// AM: Guardar cambios omitiendo el AuditService porque el usuario no esta autenticado
				await _context.SaveChangesWithoutAuditAsync();

				// AM: Mapear Entity a Dto para la respuesta
				var tagDtos = defaultTags.Select(tag => _mapper.Map<TagActivityDto>(tag)).ToList();

				return new ResponseDto<List<TagActivityDto>>
				{
					StatusCode = 201,
					Status = true,
					Message = MessagesConstant.TA_CREATE_SUCCESS,
					Data = tagDtos
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, MessagesConstant.TA_CREATE_ERROR);
				return new ResponseDto<List<TagActivityDto>>
				{
					StatusCode = 500,
					Status = false,
					Message = MessagesConstant.TA_CREATE_ERROR
				};
			}
		}
	}
}
