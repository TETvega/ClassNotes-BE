using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Cloudinary;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Audit;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CloudinaryInstance = CloudinaryDotNet.Cloudinary;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;

namespace ClassNotes.API.Services.Centers
{
	public class CentersService : ICentersService
	{
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<CentersService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ClassNotesContext _context;
        private readonly int PAGE_SIZE;
        private readonly string[] _allowedImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".webp" };      //Constantes de formatos que se pueden aceptar


        public CentersService(ClassNotesContext context,
            IMapper mapper,
			IAuditService auditService,
            ILogger<CentersService> logger,
			IConfiguration configuration) 
		{
            this._mapper = mapper;
            this._auditService = auditService;
            this._logger = logger;
            this._configuration = configuration;
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Centers");
            this._context = context;
        }


        public async Task<ResponseDto<CenterDto>> CreateAsync ([FromForm]CenterCreateDto dto, IFormFile image)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var userId = _auditService.GetUserId();

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
                            Message = MessagesConstant.NAME_REQUIRED
                        };
                    }

                    // CG: Verificar que el archivo sea una imagen
                    if(image != null)
                    {
                        if (!IsImage(image))
                        {
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = MessagesConstant.INVALID_IMAGE_FORMAT
                            };
                        }
                    }

                    var centerEntity = _mapper.Map<CenterEntity>(dto);

                    centerEntity.TeacherId = userId;

                    var nameCheck = await _context.Centers.FirstOrDefaultAsync(x => x.Name == dto.Name && x.TeacherId == centerEntity.TeacherId);

                    if (nameCheck != null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 409,
                            Status = false,
                            Message = MessagesConstant.DUPLICATE_NAME
                        };
                    }
                    
                    // CG: Se debe guardar el centro con el logo vacio porque se necesita el id del centro para que renombrar la imagen
                    _context.Centers.Add(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = new CenterDto();
                    // CG : Si la imagen no es nula entonces hay codigo relacionado a la imagen y cloudinary
                    if (image != null)
                    {
                        var uploadResult = await UploadImage(image, userId, centerEntity.Id.ToString());

                        // CG: Manejar errores relacionados al servicio externo de Cloudinary (a este punto el centro ya esta creado)
                        // CG: Verificar que no haya ningun error
                        if (uploadResult.Error != null)
                        {
                            await transaction.CommitAsync();
                            _logger.LogError($"Centro creado pero hubo error de parte de Cloudinary al asignarle imagen al logo: {uploadResult.Error.Message}");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 201,
                                Status = true,
                                Message = $"Centro creado pero no pudo asignarsele una imagen: {uploadResult.Error.Message}",
                                Data = centerDto = _mapper.Map<CenterDto>(centerEntity),
                            };
                        }

                        // CG: Verificar que el status code de la respuesta sea un 200
                        if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            await transaction.CommitAsync();
                            _logger.LogError($"Centro creado pero hubo error en la respuesta de Cloudinary: {uploadResult.StatusCode}");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 201,
                                Status = true,
                                Message = $"Centro creado pero no pudo asignarsele una imagen: {uploadResult.StatusCode}",
                                Data = centerDto = _mapper.Map<CenterDto>(centerEntity),
                            };
                        }

                        // Actualizar el campo de Logo con el url y actualizar registro
                        centerEntity.Logo = uploadResult.Url.ToString();
                        _context.Centers.Update(centerEntity);
                        await _context.SaveChangesAsync();
                    }


                    await transaction.CommitAsync();
                    centerDto = _mapper.Map<CenterDto>(centerEntity);
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
        // HR
        // Manejo del pageSize sera general para todos 

        public async Task<ResponseDto<PaginationDto<List<CenterExtendDto>>>> GetCentersListAsync(
            string searchTerm = "",
            bool? isArchived = null,
            int? pageSize = null, 
            int page = 1)
        {
            /** HR
             * Si pageSize es -1, se devuelve int.MaxValue
             * -1 significa "obtener todos los elementos", por lo que usamos int.MaxValue 
             *  int.MaxValue es 2,147,483,647, que es el valor máximo que puede tener un int en C#.
             *  Math.Max(1, valor) garantiza que currentPageSize nunca sea menor que 1 excepto el -1 al inicio
             *  si pageSize es nulo toma el valor de PAGE_SIZE
             */
            int currentPageSize = pageSize == -1 ? int.MaxValue : Math.Max(1, pageSize ?? PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;
            var userId = _auditService.GetUserId();

            //Manejo de la Query del que la pide 
            // HR -> sige siendo AsQueryable() pero ahora es implicito
            var query = _context.Centers
                .Where(x => x.TeacherId == userId);

            if (isArchived.HasValue)
            {
                query = query.Where(c => c.IsArchived == isArchived.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(x => (x.Name + " " + x.Abbreviation)
                           .ToLower().Contains(searchTerm.ToLower()));
            }

            // HR
            // Decidi realizarlo directamente en la consulta y no en el mapper 
            // Al realizarlo en el mapper tendria que cargar todos los datos en memoria y en este caso se hacen directamente en la BD
            // Consulta optimizada con los conteos
            // 
            var centersWithCounts = await query
                .OrderByDescending(x => x.CreatedDate)
                .Select(c => new CenterExtendDto
                {
                    // Mapear props básicas del centro
                    Id = c.Id,
                    Name = c.Name,
                    Abbreviation = c.Abbreviation,
                    IsArchived = c.IsArchived,
                    Logo = c.Logo,
                    TeacherId = c.TeacherId,
                    // si se ocupan mas seguir este orden


                    // Calcular totales directamente en SQL
                    TotalActiveClasses = c.Courses.Count(cl => cl.IsActive), // Cuanta las clases activas del centro
                    TotalActiveStudents = c.Courses
                        .Where(co => co.IsActive) // donde los cursos son activos
                        .SelectMany(co => co.Students) // seleciona todos los estudiantes
                        .Where(sc => sc.IsActive) // done el estudiante esta activo
                        .Select(sc => sc.StudentId) // Seleccionar solo IDs
                        .Distinct() // Estudiantes únicos
                        .Count() // retorno de conteo

                })
                .Skip(startIndex)
                .Take(currentPageSize)
                .ToListAsync();

            int totalCenters = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCenters / currentPageSize);

            return new ResponseDto<PaginationDto<List<CenterExtendDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<CenterExtendDto>>
                {
                    CurrentPage = page, // pagina actual
                    PageSize = currentPageSize, // Total de items que puede tener la peticion
                    TotalItems = totalCenters, // el total de items en la BD segun la consulta y sus filtros
                    TotalPages = totalPages, // Total de paginas que tiene la consulta con todo y filtros
                    Items = centersWithCounts,  // Los itmes y su informacion
                    HasPreviousPage = page > 1, //tiene pagina antes
                    HasNextPage = page < totalPages // tiene pagina despues
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
                            Message = MessagesConstant.DELETE_CONFIRMATION_REQUIRED
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
                            Message = MessagesConstant.UNAUTHORIZED_DELETE
                        };
                    }



                    if (courseEntity != null)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 409,
                            Status = false,
                            Message = MessagesConstant.CENTER_HAS_COURSES
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

                    //CG: Eliminar la imagen de Cloudinary relacionada antes de eliminar el registro
                    string publicId = ExtractPublicIdFromUrl(centerEntity.Logo);

                    if (publicId == "ERROR_URL_NO_VALIDO")
                    {
                        _logger.LogError("Error al tratar de eliminar la imagen, ponerse en contacto con soporte");
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 400,
                            Status = false,
                            Message = MessagesConstant.ERROR_DELETE_IMAGE
                        };
                    }

                    if (publicId != "NO_EXISTE_URL")
                    {
                        var result = await DeleteImage(publicId);

                        // CG: Manejar errores  relacionados al servicio externo de Cloudinary
                        // CG: Verificar que no exista un error
                        if (result.Error != null)
                        {
                            _logger.LogError($"Error al eliminar la imagen anterior en Cloudinary: {result.Error.Message}");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = $"Error al eliminar la imagen: {result.Error.Message}",
                            };
                        }

                        // CG: Verificar que la respuesta del result sea un "ok"
                        if (result.Result != "ok")
                        {
                            _logger.LogError($"Error en la respuesta de Cloudinary: {result.Result}");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = $"Error en la respuesta de Cloudinary: {result.Result}",
                            };
                        }
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


        // CG: La prop de changedImage permite saber si la imagen fue cambiada y asi ahorrar llamadas al API de Cloudinary
        public async Task<ResponseDto<CenterDto>> EditAsync([FromForm] CenterEditDto dto, Guid id, IFormFile image, bool changedImage)
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
                            Message = MessagesConstant.ERROR_NOT_AUTHORIZED
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
                            Message = MessagesConstant.DUPLICATE_NAME
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
                            Message = MessagesConstant.NAME_REQUIRED
                        };
                    }

                    var centerDto = new CenterDto();
                    // CG: Verificar que el archivo sea una imagen
                    if (image != null)
                    {
                        if (!IsImage(image))
                        {
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = MessagesConstant.INVALID_IMAGE_FORMAT
                            };
                        }
                    }

                    /*
                    1. Se elimina la imagen y se coloca una nueva
                        En este caso changedImage es true e image no es null, en ese caso se tiene que hacer una eliminacion de Cloudinary con el publicId, 
                        seguidamente se debe de subir la nueva imagen haciendo las respectivas validaciones, despues subir la imagen y actualizar el campo de Logo
                        con la nueva url y finalmente guardar los cambios en la DB
                     */
                    if (changedImage && image != null)
                    {
                        string publicId = ExtractPublicIdFromUrl(centerEntity.Logo);

                        if(publicId == "ERROR_URL_NO_VALIDO")
                        {
                            _logger.LogError("Error al tratar de eliminar la imagen, ponerse en contacto con soporte");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = MessagesConstant.ERROR_DELETE_IMAGE
                            };
                        }

                        //La unica forma de que no exista el publicId es que el valor de Logo sea nulo, en ese caso se omite la eliminacion
                        if (publicId != "NO_EXISTE_URL")
                        {
                            var result = await DeleteImage(publicId);

                            // CG: Manejar errores  relacionados al servicio externo de Cloudinary
                            // CG: Verificar que no exista un error
                            if (result.Error != null)
                            {
                                _logger.LogError($"Error al eliminar la imagen anterior en Cloudinary: {result.Error.Message}");
                                return new ResponseDto<CenterDto>
                                {
                                    StatusCode = 400,
                                    Status = false,
                                    Message = $"Error al eliminar la imagen: {result.Error.Message}",
                                };
                            }

                            // CG: Verificar que la respuesta del result sea un "ok"
                            if (result.Result != "ok")
                            {
                                _logger.LogError($"Error en la respuesta de Cloudinary: {result.Result}");
                                return new ResponseDto<CenterDto>
                                {
                                    StatusCode = 400,
                                    Status = false,
                                    Message = $"Error en la respuesta de Cloudinary: {result.Result}",
                                };
                            }
                        }

                        // CG: Se procede a subir la nueva imagen y despues actualizar la url de logo del centro
                        var uploadResult = await UploadImage(image, userId, centerEntity.Id.ToString());

                        // CG: Manejar errores relacionados al servicio externo de Cloudinary
                        // CG: Verificar que no haya ningun error
                        if (uploadResult.Error != null)
                        {
                            _logger.LogError($"Error al subir la imagen a Cloudinary: {uploadResult.Error.Message}");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = $"Error al subir la imagen a Cloudinary: {uploadResult.Error.Message}",
                                Data = centerDto = _mapper.Map<CenterDto>(centerEntity),
                            };
                        }

                        // CG: Verificar que el status code de la respuesta sea un 200
                        if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            _logger.LogError($"Error en la respuesta de Cloudinary:: {uploadResult.StatusCode}");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = $"Error en la respuesta de Cloudinary: {uploadResult.StatusCode}",
                                Data = centerDto = _mapper.Map<CenterDto>(centerEntity),
                            };
                        }

                        // Actualizar el campo de Logo con el url y actualizar registro
                        centerEntity.Logo = uploadResult.Url.ToString();

                    }

                    /*
                    2. Se elimina la imagen y no se coloca una nueva
                        En este caso changedImage es true y image es null, en ese caso se tiene que hacer una eliminacion de Cloudinary con el publicId, 
                        y dejar el valor de logo en null en la DB y finalmente guardar los cambios 
                    */
                    if(changedImage && image == null)
                    {
                        string publicId = ExtractPublicIdFromUrl(centerEntity.Logo);

                        if (publicId == "ERROR_URL_NO_VALIDO")
                        {
                            _logger.LogError("Error al tratar de eliminar la imagen, ponerse en contacto con soporte");
                            return new ResponseDto<CenterDto>
                            {
                                StatusCode = 400,
                                Status = false,
                                Message = MessagesConstant.ERROR_DELETE_IMAGE
                            };
                        }

                        if (publicId != "NO_EXISTE_URL")
                        {
                            var result = await DeleteImage(publicId);

                            // CG: Manejar errores  relacionados al servicio externo de Cloudinary
                            // CG: Verificar que no exista un error
                            if (result.Error != null)
                            {
                                _logger.LogError($"Error al eliminar la imagen anterior en Cloudinary: {result.Error.Message}");
                                return new ResponseDto<CenterDto>
                                {
                                    StatusCode = 400,
                                    Status = false,
                                    Message = $"Error al eliminar la imagen: {result.Error.Message}",
                                };
                            }

                            // CG: Verificar que la respuesta del result sea un "ok"
                            if (result.Result != "ok")
                            {
                                _logger.LogError($"Error en la respuesta de Cloudinary: {result.Result}");
                                return new ResponseDto<CenterDto>
                                {
                                    StatusCode = 400,
                                    Status = false,
                                    Message = $"Error en la respuesta de Cloudinary: {result.Result}",
                                };
                            }
                        }

                        centerEntity.Logo = null;
                    }

                    /*
                    3. No se elimina la imagen (el centro ya tenia una imagen como logo)
                        En este caso changedImage es false e image no es null, en ese caso solo se debe ignorar el campo de la imagen pues no se cambio y hacer los
                        cambios en los otros campos 
                        basicamente no se altera la imagen porque no se toca en la DB
                    */
                    /*
                    4. No se elimina la imagen (el centro no tenia una imagen como logo)
                        En este caso changedImage es false e image es null, en ese caso solo se debe ignorar el campo de la imagen pues no se cambios y hacer los 
                        cambios en los otros campos
                     */

                    _mapper.Map(dto, centerEntity);
                    _context.Centers.Update(centerEntity);
                    await _context.SaveChangesAsync();

                    centerDto = _mapper.Map<CenterDto>(centerEntity);

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

                    // Manejo de exepciones basicas
                    if (centerEntity.TeacherId != userId)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = MessagesConstant.UNAUTHORIZED_ARCHIVE_CENTER
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
                            Message = MessagesConstant.CENTER_ALREADY_ARCHIVED
                        };
                    }

                    // Majedo de poner todas las clases en inactive de un centro archivado
                    // https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete
                    // se realiza una query directamente donde los cursos tienen el id del centro 
                    await _context.Courses
                        .Where(course => course.CenterId == centerEntity.Id) //donde el id del centro coincide en la clase
                        .ExecuteUpdateAsync(setCourses => setCourses.SetProperty(c => c.IsActive, false)); // a ese array le ejecutamos un set donde la propiedad de es activo la pasamos a falso

                    centerEntity.IsArchived = true;

                    




                    _context.Centers.Update(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = _mapper.Map<CenterDto>(centerEntity);

                    await transaction.CommitAsync();
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = MessagesConstant.CENTER_ARCHIVED_SUCCESS,
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

        //(Ken)
        //Por cuestiones de seguridad y comodidad, es mejor que archivar y recuperar sean servicios separados...
        public async Task<ResponseDto<CenterDto>> RecoverAsync(Guid id)
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
                            Message = MessagesConstant.CENTER_RECOVERY_UNAUTHORIZED
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

                    if (!centerEntity.IsArchived)
                    {
                        return new ResponseDto<CenterDto>
                        {
                            StatusCode = 401,
                            Status = false,
                            Message = MessagesConstant.CENTER_NOT_ARCHIVED,
                        };
                    }

                    // HR
                    // cuando recupera un centro las clases donde su configuracion 
                    // la fecha de terminacion de la clase es mayor a la fecha actual las activa
                    await _context.Courses
                            .Where(course => course.CenterId == centerEntity.Id && // verifica que la clase pertenezca al centro 
                                    course.CourseSetting != null &&  // validacion de que no tenga un setting nulo (por si acaso no tendria que pasar nunca pero mejor asegutrar)
                                    course.CourseSetting.EndDate >= DateTime.UtcNow)  // valida que la fecha sea mayor a la fecha actual 
                            .ExecuteUpdateAsync(setCourses => setCourses.SetProperty(course => course.IsActive, true)); // a todos en el array del where cambia y hace un set a la proipiedad de activos 



                    centerEntity.IsArchived = false;

                    _context.Centers.Update(centerEntity);
                    await _context.SaveChangesAsync();

                    var centerDto = _mapper.Map<CenterDto>(centerEntity);

                    await transaction.CommitAsync();
                    return new ResponseDto<CenterDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = MessagesConstant.CENTER_RECOVERED_SUCCESS,
                        Data = centerDto
                    };

                }
                catch(Exception ex)
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

        // CG: Metodo para verificar que el archivo tenga extension valida
        private bool IsImage(IFormFile file)
        {

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedImageExtensions.Contains(fileExtension);
        }

        // CG: Metodo para renombrar al archivo
        private string GenerateNewFileName(string userId, string centerId)
        {
            return $"{userId}_{centerId}";
        }
    
        // CG: Metodo para obtener el publicId de la url que se encuentra en el campo de Logo del centro
        private string ExtractPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "NO_EXISTE_URL";
            }

            // Definir el segmento que identifica el inicio del publicId
            string uploadSegment = "/upload/";

            // Buscar la posición del segmento "/upload/"
            int uploadIndex = url.IndexOf(uploadSegment);
            if (uploadIndex == -1)
            {
                return "ERROR_URL_NO_VALIDO";
            }

            // Extraer la parte de la URL después de "/upload/"
            string publicIdWithVersionAndFormat = url.Substring(uploadIndex + uploadSegment.Length);

            // Eliminar la versión (si existe)
            // La versión comienza con "v" seguido de números (por ejemplo, "v1740876290/")
            int versionEndIndex = publicIdWithVersionAndFormat.IndexOf('/');
            if (versionEndIndex != -1)
            {
                publicIdWithVersionAndFormat = publicIdWithVersionAndFormat.Substring(versionEndIndex + 1);
            }

            // Eliminar la extensión del archivo (por ejemplo, ".jpg")
            int lastDotIndex = publicIdWithVersionAndFormat.LastIndexOf('.');
            if (lastDotIndex != -1)
            {
                publicIdWithVersionAndFormat = publicIdWithVersionAndFormat.Substring(0, lastDotIndex);
            }

            return publicIdWithVersionAndFormat;

        }
    
        // CG: Metodo para subir imagen al Cloudinary, retorna el tipo de dato de resultado del cloudinary y recibe por parametros la image
        private async Task<ImageUploadResult> UploadImage(IFormFile image, string userId, string centerId)
        {
            string cloudName = _configuration.GetSection("Cloudinary").GetSection("CloudName").Value;
            string apiKey = _configuration.GetSection("Cloudinary").GetSection("ApiKey").Value;
            string apiSecret = _configuration.GetSection("Cloudinary").GetSection("ApiSecret").Value;
            Account account = new Account(cloudName, apiKey, apiSecret);
            CloudinaryInstance cloudinary = new CloudinaryInstance(account);
            cloudinary.Api.Secure = true;

            // CG: Renombrar la imagen
            string newFileName = GenerateNewFileName(userId, centerId);
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var renamedFileName = $"{newFileName}{fileExtension}";

            // CG:  Carpeta temporal para almacenar la imagen
            string tempImageFolder = _configuration.GetSection("StoragePaths").GetSection("TempImageFolder").Value;
            var fileWithPath = Path.Combine(tempImageFolder, renamedFileName);
            if (!Directory.Exists(tempImageFolder))
            {
                Directory.CreateDirectory(tempImageFolder);
            }

            // CG: Guardar la imagen en la carpeta temporal
            var stream = new FileStream(fileWithPath, FileMode.Create);
            await image.CopyToAsync(stream);
            stream.Close();

            // CG: Subir la imagen a cloudinary
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileWithPath),
                UseFilename = true,     //usar nombre del archivo
                Overwrite = true,       //que se pueda sobreescribir (evitar repeticion de imagenes),
                Folder = "Centers",     //Debe existir en Cloudinary un folder de Home que se llame asi
            };
            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            // CG: Eliminar la imagen temporal (independientemente si se haya subido o no correctamente)
            System.IO.File.Delete(fileWithPath);

            return uploadResult;
        }

        // CG: Metodo para eliminar imagen del Cloudinary
        private async Task<DeletionResult> DeleteImage(string publicId)
        {
            //  CG: Obtener credenciales y crear instancia de Cloudinary
            string cloudName = _configuration.GetSection("Cloudinary").GetSection("CloudName").Value;
            string apiKey = _configuration.GetSection("Cloudinary").GetSection("ApiKey").Value;
            string apiSecret = _configuration.GetSection("Cloudinary").GetSection("ApiSecret").Value;
            Account account = new Account(cloudName, apiKey, apiSecret);
            CloudinaryInstance cloudinary = new CloudinaryInstance(account);
            cloudinary.Api.Secure = true;

            var deleteParams = new DeletionParams(publicId);
            var result = await cloudinary.DestroyAsync(deleteParams);
            return result;
        }
    }
}
