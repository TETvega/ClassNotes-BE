using ClassNotes.API.Services.CourseNotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/course-notes")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class CourseNotesController : ControllerBase
	{
		private readonly ICourseNotesService _courseNotesService;

		public CourseNotesController(ICourseNotesService courseNotesService)
        {
			this._courseNotesService = courseNotesService;
		}

		// AM: Agregar los endpoints del CRUD
	}
}
