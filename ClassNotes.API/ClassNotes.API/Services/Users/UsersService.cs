using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Users
{
	public class UsersService : IUsersService
	{
		private readonly ClassNotesContext _context;
		private readonly UserManager<UserEntity> _userManager;
		private readonly IMapper _mapper;
		private readonly ILogger<UsersService> _logger;

		public UsersService(
			ClassNotesContext context, 
			UserManager<UserEntity> userManager, 
			IMapper mapper, 
			ILogger<UsersService> logger)
        {
			this._context = context;
			this._userManager = userManager;
			this._mapper = mapper;
			this._logger = logger;
		}

		// AM: Función para editar información del usuario (nombre completo)
		public async Task<ResponseDto<UserDto>> EditAsync(UserEditDto dto, string id)
		{
			// AM: Obtener usuario y validar su existencia
			var userEntity = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
			if (userEntity is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.RECORD_NOT_FOUND
				};
			}

			// AM: Actualizar los datos del usuario
			userEntity.FirstName = dto.FirstName;
			userEntity.LastName = dto.LastName;

			// AM: Guardar los cambios
			var result = await _userManager.UpdateAsync(userEntity);
			await _context.SaveChangesAsync();

			if (!result.Succeeded)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = MessagesConstant.UPDATE_ERROR
				};
			}

			// AM: Mapear Entity a Dto para la respuesta
			var userDto = _mapper.Map<UserDto>(userEntity);

			return new ResponseDto<UserDto>
			{
				StatusCode = 200,
				Status = true,
				Message = MessagesConstant.UPDATE_SUCCESS,
				Data = userDto
			};
		}

		// AM: Función para cambiar la contraseña ingresando la actual
		public async Task<ResponseDto<UserDto>> ChangePasswordAsync(UserEditPasswordDto dto, string id)
		{
			// AM: Obtener usuario y validar su existencia
			var userEntity = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
			if (userEntity is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.RECORD_NOT_FOUND
				};
			}

			// AM: Validar que la contraseña ingresada coincide con la contraseña actual
			var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(userEntity, dto.CurrentPassword);
			if (!isCurrentPasswordValid)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "La contraseña actual es incorrecta."
				};
			}

			// AM: Actualizar la contraseña
			var passwordChangeResult = await _userManager.ChangePasswordAsync(userEntity, dto.CurrentPassword, dto.NewPassword);
			if (!passwordChangeResult.Succeeded)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "No se pudo cambiar la contraseña."
				};
			}

			// AM: Mapear Entity a Dto para la respuesta
			var userDto = _mapper.Map<UserDto>(userEntity);

			return new ResponseDto<UserDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "La contraseña fue actualizada satisfactoriamente.",
				Data = userDto
			};
		}

		// AM: Función para cambiar la contraseña mediante validación OTP
		public Task<ResponseDto<UserDto>> ChangePasswordWithOtpAsync(UserEditPasswordOtpDto dto)
		{
			throw new NotImplementedException();
		}

		// AM: Función para cambiar el correo electrónico
		public Task<ResponseDto<UserDto>> ChangeEmailAsync(UserEditEmailDto dto, string id)
		{
			throw new NotImplementedException();
		}

		// AM: Función para borrar el usuario
		public async Task<ResponseDto<UserDto>> DeleteAsync(string id)
		{
			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					throw new NotImplementedException();
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					_logger.LogError(ex, MessagesConstant.DELETE_ERROR);
					return new ResponseDto<UserDto>
					{
						StatusCode = 500,
						Status = false,
						Message = MessagesConstant.DELETE_ERROR
					};
				}
			}
		}
	}
}
