using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
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
			this._centersService = centersService;
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<ActionResult<ResponseDto<CenterDto>>> Create(CenterCreateDto dto)
		{
			var response = await _centersService.CreateAsync(dto);
            return StatusCode(response.StatusCode, response);
        }


        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<CenterDto>>> Edit(CenterEditDto dto, Guid id)
        {
			var response = await _centersService.EditAsync(dto, id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("archive/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<CenterDto>>> archive( Guid id)
        {
            var response = await _centersService.ArchiveAsync( id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
