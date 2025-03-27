using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Services.Centers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/centers")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class CentersController : ControllerBase
	{
		private readonly ICentersService _centersService;

		public CentersController(ICentersService centersService)
        {
			_centersService = centersService;
		}

		[HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CenterDto>>> Create([FromForm] CenterCreateDto dto, IFormFile image)
		{
			var response = await _centersService.CreateAsync(dto, image);
            return StatusCode(response.StatusCode, response);
        }


        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<CenterDto>>>> GetAll(string searchTerm = "", bool? isArchived = null, int page = 1)
        {
            var response = await _centersService.GetCentersListAsync(searchTerm, isArchived, page);
            return StatusCode(response.StatusCode, response);
        }


        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CenterDto>>> Get(Guid id)
        {
            var response = await _centersService.GetCenterByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }


        [HttpPut("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CenterDto>>> Edit([FromForm] CenterEditDto dto, Guid id, IFormFile image, bool changedImage)
        {
			var response = await _centersService.EditAsync(dto, id, image, changedImage);
            return StatusCode(response.StatusCode, response);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]

        public async Task<ActionResult<ResponseDto<CenterDto>>> Delete(bool confirmation, Guid id)
        {
            var response = await _centersService.DeleteAsync(confirmation, id);

            return StatusCode(response.StatusCode, response);
        }



        [HttpPut("archive/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CenterDto>>> archive( Guid id)
        {
            var response = await _centersService.ArchiveAsync( id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("recover/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CenterDto>>> recover(Guid id)
        {
            var response = await _centersService.RecoverAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
