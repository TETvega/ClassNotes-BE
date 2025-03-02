using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Cloudinary;
using ClassNotes.API.Dtos.Common;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CloudinaryInstance = CloudinaryDotNet.Cloudinary;
//se creo instancia debido a incompatibilidad entre los nombres de Cloudinary libreria y el folder del Servicio

namespace ClassNotes.API.Services.Cloudinary;
public class CloudinaryService : ICloudinaryService
{
    private readonly IConfiguration _configuration;
    private readonly ClassNotesContext _context;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly string[] _allowedImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".webp" };      //Constantes de formatos que se pueden aceptar

    public CloudinaryService(
            IConfiguration configuration,
            ClassNotesContext context,
            ILogger<CloudinaryService> logger
        )
    {
        this._configuration = configuration;
        this._context = context;
        this._logger = logger;
    }

    // CG : Metodo para subir una imagen
    public async Task<ResponseDto<CloudinaryDto>> UploadImageAsync(IFormFile image)
    {
        using(var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {

                // CG: Verificar que el archivo sea una imagen
                if (!IsImage(image))
                {
                    return new ResponseDto<CloudinaryDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = "El archivo no es una imagen válida. Formatos permitidos: .png, .jpg, .jpeg, .gif, .bmp, .tiff, .webp"
                    };
                }

                //  CG: Obtener credenciales y crear instancia de Cloudinary
                string cloudName = _configuration.GetSection("Cloudinary").GetSection("CloudName").Value;
                string apiKey = _configuration.GetSection("Cloudinary").GetSection("ApiKey").Value;
                string apiSecret = _configuration.GetSection("Cloudinary").GetSection("ApiSecret").Value;
                Account account = new Account(cloudName, apiKey, apiSecret);
                CloudinaryInstance cloudinary = new CloudinaryInstance(account);
                cloudinary.Api.Secure = true;

                // CG: Renombrar la imagen
                string newFileName = GenerateNewFileName();
                var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                var renamedFileName = $"{newFileName}{fileExtension}";
                
                // CG:  Carpeta temporal para almacenar la imagen
                string tempImageFolder = _configuration.GetSection("StoragePaths").GetSection("TempImageFolder").Value;
                //var fileName = image.FileName;        //ya no se usa este porque se renombro
                var fileWithPath = Path.Combine(tempImageFolder, renamedFileName);
                if(!Directory.Exists(tempImageFolder))
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
                    Folder = "Ejemplo",
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                // CG: Eliminar la imagen temporal (independientemente si se haya subido o no correctamente)
                System.IO.File.Delete(fileWithPath);

                // CG: Manejar errores que relacionados al servicio externo de Cloudinary
                // CG: Verificar que no haya ningun error
                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Error al subir la imagen: {uploadResult.Error.Message}");
                    return new ResponseDto<CloudinaryDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = $"Error al subir la imagen: {uploadResult.Error.Message}",
                    };
                }

                // CG: Verificar que el status code de la respuesta sea un 200
                if(uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Error en la respuesta de Cloudinary: {uploadResult.StatusCode}");
                    return new ResponseDto<CloudinaryDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = $"Error en la respuesta de Cloudinary: {uploadResult.StatusCode}",
                    };
                }

                return new ResponseDto<CloudinaryDto>
                {
                    StatusCode = 201,
                    Status = true,
                    Message = "Imagen subida correctamente",
                    Data = new CloudinaryDto
                    {
                        URL = uploadResult.Url.ToString(),
                    }
                };
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e.Message);
                return new ResponseDto<CloudinaryDto>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = e.Message,
                };
            }
        }
    }

    // CG : Metodo para borrar una imagen que este en la nube
    // CG: El publicId es el nombre del folder en Cloudinary y el nombre del archivo separado por una pleca "folder"/"archivo" (esto es lo que se guardaria en la DB)
    public async Task<ResponseDto<CloudinaryDto>> DeleteImageAsync(string publicId)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                //  CG: Obtener credenciales y crear instancia de Cloudinary
                string cloudName = _configuration.GetSection("Cloudinary").GetSection("CloudName").Value;
                string apiKey = _configuration.GetSection("Cloudinary").GetSection("ApiKey").Value;
                string apiSecret = _configuration.GetSection("Cloudinary").GetSection("ApiSecret").Value;
                Account account = new Account(cloudName, apiKey, apiSecret);
                CloudinaryInstance cloudinary = new CloudinaryInstance(account);
                cloudinary.Api.Secure = true;

                // CG: Eliminar la imagen en Cloudinary
                var deleteParams = new DeletionParams(publicId);
                var result = await cloudinary.DestroyAsync(deleteParams);

                // CG: Manejar errores que relacionados al servicio externo de Cloudinary
                // CG: Verificar que no exista un error
                if(result.Error != null)
                {
                    _logger.LogError($"Error al eliminar la imagen en Cloudinary: {result.Error.Message}");
                    return new ResponseDto<CloudinaryDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = $"Error al eliminar la imagen: {result.Error.Message}",
                    };
                }

                // CG: Verificar que la respuesta del result sea un "ok"
                if(result.Result != "ok")
                {
                    _logger.LogError($"Error en la respuesta de Cloudinary: {result.Result}");
                    return new ResponseDto<CloudinaryDto>
                    {
                        StatusCode = 500,
                        Status = false,
                        Message = $"Error en la respuesta de Cloudinary: {result.Result}",
                    };
                }

                return new ResponseDto<CloudinaryDto>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "Imagen eliminada correctamente",
                };
            }
            catch(Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e.Message);
                return new ResponseDto<CloudinaryDto>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = e.Message,
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
    private string GenerateNewFileName()
    {
        // CG: En el metodo verdadero debe ser una combinacion entre el id del centro y el id del usuario registrado (recibirlo por parametros)
        return $"imagenrenombrada_desde_servicio_{Guid.NewGuid()}";
    }
}
