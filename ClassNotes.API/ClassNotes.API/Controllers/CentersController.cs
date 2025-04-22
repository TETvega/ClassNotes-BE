using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.DashboarCenter;
using ClassNotes.API.Services.Centers;
using ClassNotes.API.Services.DashboarCenter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/centers")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class CentersController : ControllerBase
	{
        private readonly IDashboardCenterService _dashboardHomeService;
        private readonly ICentersService _centersService;

		public CentersController(
            IDashboardCenterService dashboardHomeService,
            ICentersService centersService)
        {
            _dashboardHomeService = dashboardHomeService;
            _centersService = centersService;
		}

        [HttpGet("info/{centerid}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<DashboardCenterDto>>> GetDashboardInfo(Guid centerId, string searchTerm = "", int page = 1, int? pageSize = null, string classType = null)
        {
            var result = await _dashboardHomeService.GetDashboardCenterAsync(centerId, searchTerm, page, pageSize, classType);
            return StatusCode(result.StatusCode, result);
        }


        [HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<CenterDto>>> Create([FromForm] CenterCreateDto dto, IFormFile image)
		{
			var response = await _centersService.CreateAsync(dto, image);
            return StatusCode(response.StatusCode, response);
        }


        [HttpGet("actives")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<CenterDto>>>>> GetCentersActivesNames( int? pageSize = null, int page = 1)
        {
            var response = await _centersService.GetCentersActivesListAsync(pageSize, page);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<CenterExtendDto>>>> GetAll(string searchTerm = "", bool? isArchived = null, int? pageSize = null, int page = 1)
        {
            var response = await _centersService.GetCentersListAsync(searchTerm, isArchived, pageSize,page);
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
