using ClassNotes.API.Dtos.Cloudinary;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Cloudinary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers;

[Route("api/cloudinary")]
[ApiController]
public class CloudinaryController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;

    public CloudinaryController(ICloudinaryService cloudinaryService)
    {
        this._cloudinaryService = cloudinaryService;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ResponseDto<CloudinaryDto>>> UploadImage(IFormFile image)
    {
        var response = await _cloudinaryService.UploadImageAsync(image);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("delete")]
    public async Task<ActionResult<ResponseDto<CloudinaryDto>>> DeleteImage(string publicId)
    {
        var response = await _cloudinaryService.DeleteImageAsync(publicId);
        return StatusCode(response.StatusCode, response);
    }

}
