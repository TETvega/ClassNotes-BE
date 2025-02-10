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

		// AM: Agregar los endpoints del CRUD
	}
}
