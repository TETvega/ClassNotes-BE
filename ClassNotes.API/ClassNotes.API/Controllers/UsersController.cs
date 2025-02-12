using ClassNotes.API.Constants;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Users;
using ClassNotes.API.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassNotes.API.Controllers
{
	[ApiController]
	[Route("api/users")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	public class UsersController : ControllerBase
	{
		private readonly IUsersService _usersService;

		public UsersController(IUsersService usersService)
        {
			this._usersService = usersService;
		}

		[HttpPut("{id}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<UserDto>>> Edit(UserEditDto dto, string id)
		{
			var response = await _usersService.EditAsync(dto, id);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPut("password/{id}")]
		[Authorize(Roles = $"{RolesConstant.USER}")]
		public async Task<ActionResult<ResponseDto<UserDto>>> ChangePassword(UserEditPasswordDto dto, string id)
		{
			var response = await _usersService.ChangePasswordAsync(dto, id);
			return StatusCode(response.StatusCode, response);
		}

		[HttpPut("password-otp")]
		[AllowAnonymous]
		public async Task<ActionResult<ResponseDto<UserDto>>> ChangePasswordOtp(UserEditPasswordOtpDto dto)
		{
			var response = await _usersService.ChangePasswordWithOtpAsync(dto);
			return StatusCode(response.StatusCode, response);
		}
	}
}
