using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Services.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	// --------------------- CP --------------------- //
    [ApiController]
    [Route("api/activities")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ActivitiesController : ControllerBase
    {
        private readonly IActivitiesService _activitiesService;
        public ActivitiesController(IActivitiesService activitiesService)
        {
            _activitiesService = activitiesService;
        }

        // Traer todas las actividades (Con paginación)
        [HttpGet]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<ActivityDto>>>> GetAll(
            string searchTerm = "",
            int page = 1
        )
        {
            var response = await _activitiesService.GetActivitiesListAsync(searchTerm, page);
            return StatusCode(response.StatusCode, response);
        }
    }
}