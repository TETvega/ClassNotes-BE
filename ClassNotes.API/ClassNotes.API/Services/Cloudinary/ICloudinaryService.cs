using ClassNotes.API.Dtos.Cloudinary;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Services.Cloudinary;

public interface ICloudinaryService
{
    Task<ResponseDto<CloudinaryDto>> DeleteImageAsync(string publicId);
    Task<ResponseDto<CloudinaryDto>> UploadImageAsync(IFormFile image);
}
