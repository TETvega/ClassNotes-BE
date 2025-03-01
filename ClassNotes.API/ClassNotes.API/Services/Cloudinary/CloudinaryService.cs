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
                string cloudName = _configuration.GetSection("Cloudinary").GetSection("CloudName").Value;
                string apiKey = _configuration.GetSection("Cloudinary").GetSection("ApiKey").Value;
                string apiSecret = _configuration.GetSection("Cloudinary").GetSection("ApiSecret").Value;
                Account account = new Account(cloudName, apiKey, apiSecret);
                CloudinaryInstance cloudinary = new CloudinaryInstance(account);
                cloudinary.Api.Secure = true;

                string tempImageFolder = _configuration.GetSection("StoragePaths").GetSection("TempImageFolder").Value;
                var fileName = image.FileName;
                var fileWithPath = Path.Combine(tempImageFolder, fileName);

                if(!Directory.Exists(tempImageFolder))
                {
                    Directory.CreateDirectory(tempImageFolder);
                }

                var stream = new FileStream(fileWithPath, FileMode.Create);
                await image.CopyToAsync(stream);
                stream.Close();

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileWithPath),
                    UseFilename = true,     //usar nombre del archivo
                    Overwrite = true,       //que se pueda sobreescribir (evitar repeticion de imagenes),
                    Folder = "Ejemplo",
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);
                System.IO.File.Delete(fileWithPath);

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
                string cloudName = _configuration.GetSection("Cloudinary").GetSection("CloudName").Value;
                string apiKey = _configuration.GetSection("Cloudinary").GetSection("ApiKey").Value;
                string apiSecret = _configuration.GetSection("Cloudinary").GetSection("ApiSecret").Value;
                Account account = new Account(cloudName, apiKey, apiSecret);
                CloudinaryInstance cloudinary = new CloudinaryInstance(account);
                cloudinary.Api.Secure = true;

                var deleteParams = new DeletionParams(publicId);
                var result = await cloudinary.DestroyAsync(deleteParams);

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
}
