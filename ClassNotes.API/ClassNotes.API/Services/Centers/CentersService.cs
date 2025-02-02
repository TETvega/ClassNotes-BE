using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using Microsoft.AspNetCore.Http;

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

                    if (dto.Abbreviation.Trim() == "")
                    {
                        dto.Abbreviation = null;
                    }

                    if (dto.Name.Trim() == "" )
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 400,
                            Status = false,
                            Message = "El nombre es requerido."
                        };
                    }

                    if (dto.Logo.Trim() == "")
                    {
                        dto.Logo = null;
                    }

                    var centerEntity = _mapper.Map<CenterEntity>(dto);



                    centerEntity.TeacherId = _auditService.GetUserId();

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

        public async Task<ResponseDto<CenterDto>> EditAsync(CenterEditDto dto, Guid id)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    var userId = _auditService.GetUserId();
                    var centerEntity = await _context.Centers.FindAsync(id);

                    if (centerEntity is null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 404,
                            Status = false,
                            Message = MessagesConstant.RECORD_NOT_FOUND + " " + id,
                        };
                    }

                    //(Ken)
                    //Aun quedaria probarlo con autenticación...
                    if (centerEntity.TeacherId != userId)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = "No esta autorizado para editar este registro."
                        };
                    }

                    if (dto.Abbreviation.Trim() == "")
                    {
                        dto.Abbreviation = null;
                    }

                    if (dto.Name.Trim() == "")
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 400,
                            Status = false,
                            Message = "El nombre es requerido."
                        };
                    }

                    if (dto.Logo.Trim() == "")
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
    }
}
