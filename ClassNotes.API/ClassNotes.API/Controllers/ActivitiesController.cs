using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Activities;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Algorithm;

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
        public async Task<ActionResult<ResponseDto<List<ActivityDto>>>> GetAllActivities(
            string searchTerm = "",
            int page = 1,
            int pageSize = 10,
            Guid? centerId = null,
            Guid? tagActivityId = null,
            string typeActivities = "ALL"
        )
        {
            var response = await _activitiesService.GetActivitiesListAsync(searchTerm, page, pageSize, centerId, tagActivityId, typeActivities);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("student-pendings")]
        [Authorize(Roles =$"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<PaginationDto<List<ActivityDto>>>>> GetStudentPendingsListAsync(Guid studentId,Guid courseId,int page = 1, int? pageSize = 10)
        {
            var response = await _activitiesService.GetStudentPendingsListAsync(studentId, courseId, page, pageSize);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("student-info")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<StudentAndPendingsDto>>> GetStudentPendingsinfoAsync(Guid studentId, Guid courseId)
        {
            var response = await _activitiesService.GetStudentPendingsInfoAsync(studentId, courseId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("course/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<ActivityResponseDto>>>> GetAllActivitiesByClass(
            Guid id,
            string searchTerm = "",
            int page = 1,
            int pageSize = 10,
            Guid? tagActivityId = null,
            Guid? unitId = null,
            string typeActivities = "ALL",
            string isExtraFilter = "ALL"
)
        {
            var response = await _activitiesService.GetAllActivitiesByClassAsync(id, searchTerm, page, pageSize, tagActivityId, unitId,typeActivities, isExtraFilter);
            return StatusCode(response.StatusCode, response);
        }


        //(Ken)
        [HttpGet("students_scores/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<List<StudentAndNoteDto>>>> GetStudentAndScoreAsync(Guid id, int page = 1, string searchTerm = "", int? pageSize = null)
        {
            var response = await _activitiesService.GetStudentsActivityScoreAsync(id, page, searchTerm, pageSize);
            return StatusCode(response.StatusCode, response);
        }




        // Traer una actividad mediante su id
        [HttpGet("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<ActivityDto>>> GetActivityByID(Guid id)
        {
            var response = await _activitiesService.GetActivityByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        // Crear una actividad
        [HttpPost]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<ActivityDto>>> Create(ActivityCreateDto dto)
        {
            var response = await _activitiesService.CreateAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("review/{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<ActivityDto>>> Review(List<StudentActivityNoteCreateDto> dto, Guid id)
        {
            var response = await _activitiesService.ReviewActivityAsync(dto, id);
            return StatusCode(response.StatusCode, response);
        }

        // Editar una actividad
        [HttpPut("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<ActivityDto>>> Edit(ActivityEditDto dto, Guid id)
        {
            var response = await _activitiesService.EditAsync(dto, id);
            return StatusCode(response.StatusCode, response);
        }

        // Eliminar una actividad
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task<ActionResult<ResponseDto<ActivityDto>>> Delete(Guid id)
        {
            var response = await _activitiesService.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}