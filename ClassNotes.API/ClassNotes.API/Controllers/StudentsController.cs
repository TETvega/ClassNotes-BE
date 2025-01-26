using ClassNotes.API.Services.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/students")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class StudentsController : ControllerBase
	{
		private readonly IStudentsService _studentsService;

		public StudentsController(IStudentsService studentsService)
        {
			this._studentsService = studentsService;
		}

		// AM: Agregar los endpoints del CRUD
	}
}
