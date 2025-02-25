using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Centers
{
	public class CentersService : ICentersService
	{
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<CentersService> _logger;
        private readonly ClassNotesContext _context;
        private readonly int PAGE_SIZE;

        public CentersService(ClassNotesContext context,
            IMapper mapper,
			IAuditService auditService,
            ILogger<CentersService> logger,
			IConfiguration configuration) 
		{
            this._mapper = mapper;
            this._auditService = auditService;
            this._logger = logger;
            PAGE_SIZE = configuration.GetValue<int>("PageSize");
            this._context = context;
        }


        public async Task<ResponseDto<CenterDto>> CreateAsync (CenterCreateDto dto)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    if (dto.Abbreviation?.Trim() == "")
                    {
                        dto.Abbreviation = null;
                    }

                    if (dto.Name?.Trim() == "" )
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 400,
                            Status = false,
                            Message = "El nombre es requerido."
                        };
                    }

                    if (dto.Logo?.Trim() == "")
                    {
                        dto.Logo = null;
                    }


                    var centerEntity = _mapper.Map<CenterEntity>(dto);

                    centerEntity.TeacherId = _auditService.GetUserId();

                    var nameCheck = await _context.Centers.FirstOrDefaultAsync(x => x.Name == dto.Name && x.TeacherId == centerEntity.TeacherId);

                    if (nameCheck != null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 409,
                            Status = false,
                            Message = "Ya existe un centro con este nombre, ingrese uno nuevo."
                        };
                    }



                    _context.Centers.Add(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = _mapper.Map<CenterDto>(centerEntity);

                    await transaction.CommitAsync();
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 201,
                        Status = true,
                        Message = MessagesConstant.CREATE_SUCCESS,
                        Data = centerDto
                    };
                }

                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, MessagesConstant.CREATE_ERROR);
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = MessagesConstant.CREATE_ERROR
                    };
                }
            }
        }

         //(ken)
        //Por Ahora este metodo separa completamente los centros archivados y no archivados, quiza cambie si se quiere la opcion de recibirlos todos en una sola llamada...
        public async Task<ResponseDto<PaginationDto<List<CenterDto>>>> GetCentersListAsync(string searchTerm = "",bool isArchived = false,int page = 1)
        {
            int startIndex = (page - 1) * PAGE_SIZE;

            var userId = _auditService.GetUserId();

            var centersQuery = _context.Centers.AsQueryable().Where(x => x.IsArchived == isArchived && x.TeacherId == userId);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                centersQuery = centersQuery
                    .Where(x => (x.Name + " " + x.Abbreviation)
                    .ToLower().Contains(searchTerm.ToLower()));
            }

            int totalCenters = await centersQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCenters / PAGE_SIZE);

            var centersEntity = await centersQuery
                .OrderByDescending(x => x.CreatedDate)
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var centersDto = _mapper.Map<List<CenterDto>>(centersEntity);

            return new ResponseDto<PaginationDto<List<CenterDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<CenterDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalCenters,
                    TotalPages = totalPages,
                    Items = centersDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }

        public async Task<ResponseDto<CenterDto>> GetCenterByIdAsync(Guid id)
        {
            var userId = _auditService.GetUserId();
            var centerEntity = await _context.Centers.FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == userId);
            if (centerEntity == null)
            {
                return new ResponseDto<CenterDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }
            var centerDto = _mapper.Map<CenterDto>(centerEntity);
            return new ResponseDto<CenterDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORD_FOUND,
                Data = centerDto
            };
        }



        public async Task<ResponseDto<CenterDto>> DeleteAsync(bool confirmation,  Guid id)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (!confirmation)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 409,
                            Status = false,
                            Message = "No se confirmó la eliminación del centro."
                        };
                    }

                    var courseEntity = await _context.Courses.FirstOrDefaultAsync(a => a.CenterId == id);
                    var userId = _auditService.GetUserId();
                    var centerEntity = await _context.Centers.FindAsync(id);

                    if (centerEntity.TeacherId != userId)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = "No esta autorizado para borrar este registro."
                        };
                    }



                    if (courseEntity != null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 409,
                            Status = false,
                            Message = "No se puede eliminar un centro si aún contiene clases asignadas."
                        };
                    }


                    if (centerEntity is null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 404,
                            Status = false,
                            Message = MessagesConstant.RECORD_NOT_FOUND + " " + id,
                        };
                    }



                    _context.Centers.Remove(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = _mapper.Map<CenterDto>(centerEntity);

                    await transaction.CommitAsync();
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = MessagesConstant.DELETE_SUCCESS,

                    };
                }

                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, MessagesConstant.DELETE_ERROR);
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = MessagesConstant.DELETE_ERROR

                    };
                }
            }
        }



        public async Task<ResponseDto<CenterDto>> EditAsync(CenterEditDto dto, Guid id)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    var userId = _auditService.GetUserId();
                    var centerEntity = await _context.Centers.FindAsync(id);



                    if (centerEntity.TeacherId != userId)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = "No esta autorizado para editar este registro."
                        };
                    }


                    if (centerEntity is null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 404,
                            Status = false,
                            Message = MessagesConstant.RECORD_NOT_FOUND + " " + id,
                        };
                    }

                    var nameCheck = await _context.Centers.FirstOrDefaultAsync(x => x.Name==dto.Name && x.TeacherId==centerEntity.TeacherId);

                    if (nameCheck != null && nameCheck.Id != id)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 409,
                            Status = false,
                            Message = "Ya existe un centro con este nombre, ingrese uno nuevo."
                        };
                    }


                    if (dto.Abbreviation?.Trim() == "")
                    {
                        dto.Abbreviation = null;
                    }

                    if (dto.Name?.Trim() == "")
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 400,
                            Status = false,
                            Message = "El nombre es requerido."
                        };
                    }

                    if (dto.Logo?.Trim() == "")
                    {
                        dto.Logo = null;
                    }


                    _mapper.Map(dto, centerEntity);
                    _context.Centers.Update(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = _mapper.Map<CenterDto>(centerEntity);

                    await transaction.CommitAsync();
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = MessagesConstant.UPDATE_SUCCESS,
                        Data = centerDto
                    };
                }

                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, MessagesConstant.UPDATE_ERROR);
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = MessagesConstant.UPDATE_ERROR

                    };
                }
            }
        }

        public async Task<ResponseDto<CenterDto>> ArchiveAsync( Guid id)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    var userId = _auditService.GetUserId();
                    var centerEntity = await _context.Centers.FindAsync(id);


                    if (centerEntity.TeacherId != userId)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = "No esta autorizado para archivar este centro."
                        };
                    }


                    if (centerEntity is null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 404,
                            Status = false,
                            Message = MessagesConstant.RECORD_NOT_FOUND + " " + id,
                        };
                    }

                    if (centerEntity.IsArchived)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = "Ya archivó este centro."
                        };
                    }
                


                    centerEntity.IsArchived = true;

                    _context.Centers.Update(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = _mapper.Map<CenterDto>(centerEntity);

                    await transaction.CommitAsync();
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = "Se archivó correctamente.",
                        Data = centerDto
                    };
                }

                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, MessagesConstant.UPDATE_ERROR);
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = MessagesConstant.UPDATE_ERROR

                    };
                }
            }
        }
    }
}
