using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.TagsActivities;
using ClassNotes.API.Services.TagsActivities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
    [ApiController]
    [Route("api/tags_activities")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class TagsActivitiesController : ControllerBase
    {
		private readonly ITagsActivitiesService _tagsActivitiesService;

		public TagsActivitiesController(ITagsActivitiesService tagsActivitiesService)
		{
			this._tagsActivitiesService = tagsActivitiesService;
		}

		[HttpGet]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<List<TagActivityDto>>>> PaginationList(string searchTerm = "", int page = 1)
		{
			var response = await _tagsActivitiesService.GetTagsListAsync(searchTerm, page);
			return StatusCode(response.StatusCode, response);
		}

		[HttpGet("{id}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<TagActivityDto>>> Get(Guid id)
		{
			var response = await _tagsActivitiesService.GetTagByIdAsync(id);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPost]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<TagActivityDto>>> Create(TagActivityCreateDto dto)
		{
			var response = await _tagsActivitiesService.CreateTagAsync(dto);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPut("{id}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<TagActivityDto>>> Edit(TagActivityEditDto dto, Guid id)
		{
			var response = await _tagsActivitiesService.UpdateTagAsync(dto, id);
			return StatusCode(response.StatusCode, response);
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<TagActivityDto>>> Delete(Guid id)
		{
			var response = await _tagsActivitiesService.DeleteTagAsync(id);
			return StatusCode(response.StatusCode, response);
		}
	}
}
