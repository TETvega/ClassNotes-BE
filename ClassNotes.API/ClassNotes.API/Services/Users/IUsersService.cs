using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Users;

namespace ClassNotes.API.Services.Users
{
	public interface IUsersService
	{
		Task<ResponseDto<UserDto>> EditAsync(UserEditDto dto, string id);
		Task<ResponseDto<UserDto>> ChangePasswordAsync(UserEditPasswordDto dto, string id);
		Task<ResponseDto<UserDto>> ChangePasswordWithOtpAsync(UserEditPasswordOtpDto dto);
		Task<ResponseDto<UserDto>> ChangeEmailAsync(UserEditEmailDto dto, string id);
		Task<ResponseDto<UserDto>> DeleteAsync(string id);
	}
}
